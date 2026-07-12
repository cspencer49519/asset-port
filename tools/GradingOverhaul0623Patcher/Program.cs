using Mono.Cecil;
using Mono.Cecil.Cil;

const string backupName = "Grading Overhaul.dll.original-0623";
const string outputName = "Grading Overhaul.patched.dll";

var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var assetPort = Path.GetFullPath(Path.Combine(projectDir, "..", ".."));
var workspace = Path.GetFullPath(Path.Combine(assetPort, ".."));
var sourcePath = Path.Combine(
    workspace,
    "TCG Card Shop Simulator-0.62.3",
    "BepInEx",
    "plugins",
    "Grading Overhaul",
    backupName);
var gamePath = Path.Combine(
    workspace,
    "TCG Card Shop Simulator-0.62.3",
    "Card Shop Simulator_Data",
    "Managed",
    "Assembly-CSharp.dll");
var outputPath = Path.Combine(assetPort, "tools", outputName);

if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine($"Missing backup: {sourcePath}");
    return 1;
}

if (!File.Exists(gamePath))
{
    Console.Error.WriteLine($"Missing game assembly: {gamePath}");
    return 1;
}

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(gamePath)!);
var reader = new ReaderParameters { AssemblyResolver = resolver };

using var gameAssembly = AssemblyDefinition.ReadAssembly(gamePath, reader);
var gameTypes = new HashSet<string>(
    gameAssembly.MainModule.Types.Select(static type => type.Name),
    StringComparer.Ordinal);

using var assembly = AssemblyDefinition.ReadAssembly(sourcePath, reader);

var textureReplacerPath = Path.Combine(
    workspace,
    "TCG Card Shop Simulator-0.62.3",
    "BepInEx",
    "plugins",
    "TextureReplacer",
    "TextureReplacer.dll");

// Patches that target methods missing or with incompatible signatures on 0.62.3.
var forceDisabledTypes = new HashSet<string>(StringComparer.Ordinal)
{
    "Card3dUIGroup_SetSimplifyCardDistanceCull_Patch",
    "CardUI_GradedCardOcclusionCull_Patch",
    // Broken m_IsOpeningBox ordering on 0.62.3; vanilla open path is safer.
    "GradingBoxRevealOpenPatch",
    // Calls SpawnCardVisualsNow → missing SetSimplifyCardDistanceCull on 0.62.3.
    "BoxCardRevealRestorePatch",
    // Defers card spawn; relies on broken SpawnCardVisualsNow on open.
    "BoxCardCapacityPatch",
};

if (!TextureReplacerHasApplyCustomFont(textureReplacerPath, reader))
{
    forceDisabledTypes.Add("TextureReplacer_FontBlock_Patch");
    Console.WriteLine("  TextureReplacer.ApplyCustomFont missing — will disable TextureReplacer_FontBlock_Patch");
}

var methodRenames = new Dictionary<string, string>(StringComparer.Ordinal)
{
    ["ShowSimplifiedCullingGradedCardCase"] = "ShowGradedCardCase",
};

var disabled = 0;
var renamedMethods = 0;

foreach (var type in assembly.MainModule.Types)
{
    ProcessType(type);
}

var removedMissingCalls = RemoveMissingCard3dUiGroupCalls(assembly);
if (removedMissingCalls > 0)
{
    Console.WriteLine($"  removed {removedMissingCalls} SetSimplifyCardDistanceCull call(s) for 0.62.3");
}

void ProcessType(TypeDefinition type)
{
    if (ShouldDisablePatchClass(type))
    {
        RemoveHarmonyPatchAttributes(type);
        disabled++;
        Console.WriteLine($"  disabled patch class {type.FullName}");
        return;
    }

    RemoveHarmonyPatchAttributesOnType(type, methodRenames, ref renamedMethods);

    foreach (var method in type.Methods)
    {
        renamedMethods += RenameHarmonyPatchTargets(method, methodRenames);
        RenameMethodReferences(method, methodRenames);
    }

    foreach (var nested in type.NestedTypes)
    {
        ProcessType(nested);
    }
}

bool ShouldDisablePatchClass(TypeDefinition type)
{
    if (forceDisabledTypes.Contains(type.Name))
    {
        return true;
    }

    if (ReferencesMissingGameType(type.CustomAttributes))
    {
        return true;
    }

    foreach (var method in type.Methods)
    {
        if (ReferencesMissingGameType(method.CustomAttributes))
        {
            return true;
        }
    }

    return false;
}

bool ReferencesMissingGameType(IEnumerable<CustomAttribute> attributes)
{
    foreach (var attr in attributes)
    {
        if (!IsHarmonyPatchAttribute(attr))
        {
            continue;
        }

        if (attr.ConstructorArguments.Count == 0)
        {
            continue;
        }

        var first = attr.ConstructorArguments[0];
        if (first.Type.FullName != "System.Type" || first.Value is not TypeReference typeRef)
        {
            continue;
        }

        if (!IsGameTypeReference(typeRef))
        {
            continue;
        }

        if (!gameTypes.Contains(typeRef.Name))
        {
            Console.WriteLine($"    missing game type: {typeRef.Name}");
            return true;
        }
    }

    return false;
}

static bool IsGameTypeReference(TypeReference typeRef)
{
    return typeRef.Scope is AssemblyNameReference asmRef
        && asmRef.Name == "Assembly-CSharp";
}

var writer = new WriterParameters { WriteSymbols = false };
assembly.Write(outputPath, writer);
Console.WriteLine($"Wrote {outputPath}");
Console.WriteLine($"  disabled classes: {disabled}");
Console.WriteLine($"  renamed patch targets: {renamedMethods}");
return 0;

static int RemoveMissingCard3dUiGroupCalls(AssemblyDefinition assembly)
{
    var count = 0;
    foreach (var type in assembly.MainModule.Types)
    {
        count += RemoveMissingCard3dUiGroupCallsFromType(type);
    }

    return count;
}

static int RemoveMissingCard3dUiGroupCallsFromType(TypeDefinition type)
{
    var count = 0;
    foreach (var method in type.Methods)
    {
        if (!method.HasBody)
        {
            continue;
        }

        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt)
            {
                continue;
            }

            if (instruction.Operand is not MethodReference methodRef)
            {
                continue;
            }

            if (methodRef.DeclaringType?.Name != "Card3dUIGroup" ||
                methodRef.Name != "SetSimplifyCardDistanceCull")
            {
                continue;
            }

            var previous = instruction.Previous;
            if (previous != null)
            {
                if (previous.OpCode == OpCodes.Ldc_I4_0 ||
                    previous.OpCode == OpCodes.Ldc_I4_1 ||
                    previous.OpCode == OpCodes.Ldc_I4_S ||
                    previous.OpCode == OpCodes.Ldc_I4)
                {
                    previous.OpCode = OpCodes.Nop;
                    previous.Operand = null;
                }
            }

            instruction.OpCode = OpCodes.Pop;
            instruction.Operand = null;
            count++;
        }
    }

    foreach (var nested in type.NestedTypes)
    {
        count += RemoveMissingCard3dUiGroupCallsFromType(nested);
    }

    return count;
}

static bool TextureReplacerHasApplyCustomFont(string pluginPath, ReaderParameters reader)
{
    if (!File.Exists(pluginPath))
    {
        return false;
    }

    using var plugin = AssemblyDefinition.ReadAssembly(pluginPath, reader);
    foreach (var type in plugin.MainModule.Types)
    {
        foreach (var method in type.Methods)
        {
            if (method.Name == "ApplyCustomFont")
            {
                return true;
            }
        }
    }

    return false;
}

static void RemoveHarmonyPatchAttributes(TypeDefinition type)
{
    RemoveHarmonyAttributes(type.CustomAttributes);
    foreach (var method in type.Methods)
    {
        RemoveHarmonyAttributes(method.CustomAttributes);
    }
}

static void RemoveHarmonyAttributes(Mono.Collections.Generic.Collection<CustomAttribute> attributes)
{
    for (var i = attributes.Count - 1; i >= 0; i--)
    {
        if (IsHarmonyPatchAttribute(attributes[i]))
        {
            attributes.RemoveAt(i);
        }
    }
}

static void RemoveHarmonyPatchAttributesOnType(
    TypeDefinition type,
    IReadOnlyDictionary<string, string> methodRenames,
    ref int renamedMethods)
{
    foreach (var attr in type.CustomAttributes.Where(IsHarmonyPatchAttribute).ToList())
    {
        if (TryRenameHarmonyPatchAttribute(attr, methodRenames))
        {
            renamedMethods++;
        }
    }
}

static int RenameHarmonyPatchTargets(MethodDefinition method, IReadOnlyDictionary<string, string> methodRenames)
{
    var count = 0;
    foreach (var attr in method.CustomAttributes.Where(IsHarmonyPatchAttribute).ToList())
    {
        if (TryRenameHarmonyPatchAttribute(attr, methodRenames))
        {
            count++;
        }
    }

    return count;
}

static bool IsHarmonyPatchAttribute(CustomAttribute attr)
{
    var name = attr.AttributeType.FullName;
    return name is "HarmonyLib.HarmonyPatch" or "HarmonyLib.HarmonyPatchAttribute";
}

static bool TryRenameHarmonyPatchAttribute(CustomAttribute attr, IReadOnlyDictionary<string, string> methodRenames)
{
    if (attr.ConstructorArguments.Count < 2)
    {
        return false;
    }

    var first = attr.ConstructorArguments[0];
    if (first.Type.FullName != "System.Type" || first.Value is not TypeReference)
    {
        return false;
    }

    var second = attr.ConstructorArguments[1];
    if (second.Value is not string methodName || !methodRenames.TryGetValue(methodName, out var replacement))
    {
        return false;
    }

    attr.ConstructorArguments[1] = new CustomAttributeArgument(second.Type, replacement);
    return true;
}

static void RenameMethodReferences(MethodDefinition method, IReadOnlyDictionary<string, string> methodRenames)
{
    if (!method.HasBody)
    {
        return;
    }

    foreach (var instruction in method.Body.Instructions)
    {
        if (instruction.Operand is MethodReference methodRef &&
            methodRenames.TryGetValue(methodRef.Name, out var replacement))
        {
            methodRef.Name = replacement;
        }
    }
}
