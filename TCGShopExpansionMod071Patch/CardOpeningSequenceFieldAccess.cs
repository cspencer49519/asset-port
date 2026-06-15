using System;
using System.Collections.Generic;
using System.Reflection;

namespace TCGShopExpansionMod071Patch;

internal static class CardOpeningSequenceFieldAccess
{
    private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly Dictionary<string, FieldInfo> Fields = new(StringComparer.Ordinal);

    public static object? GetValue(CardOpeningSequence sequence, string fieldName)
    {
        FieldInfo? field = GetField(fieldName);
        return field?.GetValue(sequence);
    }

    public static void SetValue(CardOpeningSequence sequence, string fieldName, object? value)
    {
        FieldInfo? field = GetField(fieldName);
        field?.SetValue(sequence, value);
    }

    private static FieldInfo? GetField(string fieldName)
    {
        if (Fields.TryGetValue(fieldName, out FieldInfo cached))
        {
            return cached;
        }

        FieldInfo? field = typeof(CardOpeningSequence).GetField(fieldName, InstanceAny);
        if (field != null)
        {
            Fields[fieldName] = field;
        }

        return field;
    }
}
