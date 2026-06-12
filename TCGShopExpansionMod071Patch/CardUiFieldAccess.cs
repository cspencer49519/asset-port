using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TCGShopExpansionMod071Patch;

internal static class CardUiFieldAccess
{
    private const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly Dictionary<string, FieldInfo> Fields = new(StringComparer.Ordinal);

    public static object? GetValue(CardUI cardUi, string fieldName)
    {
        FieldInfo? field = GetField(fieldName);
        return field?.GetValue(cardUi);
    }

    private static FieldInfo? GetField(string fieldName)
    {
        if (Fields.TryGetValue(fieldName, out FieldInfo cached))
        {
            return cached;
        }

        FieldInfo? field = typeof(CardUI).GetField(fieldName, InstanceAny);
        if (field != null)
        {
            Fields[fieldName] = field;
        }

        return field;
    }
}
