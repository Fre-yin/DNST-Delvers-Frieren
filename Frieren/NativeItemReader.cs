using System.Runtime.InteropServices;
using Il2CppInterop.Runtime;
using ItemList = Il2CppSystem.Collections.Generic.List<Il2CppSystem.ValueTuple<string, int>>;

namespace FrierenPortrait;

// Reads a native List<ValueTuple<string, int>> straight from IL2CPP memory.
// Il2CppInterop's value-tuple handling is what broke under BepInEx, so checking
// a native item list must not depend on it. Every offset is validated against
// the element size before memory is read.
internal static class NativeItemReader
{
    internal static bool TryRead(ItemList list, out List<(string Key, int Count)> items, out string error)
    {
        items = new List<(string Key, int Count)>();
        error = null;
        try
        {
            if (list == null) { error = "no list"; return false; }
            var listPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(list);
            var listClass = IL2CPP.il2cpp_object_get_class(listPtr);
            var array = Marshal.ReadIntPtr(listPtr + FieldOffset(listClass, "_items"));
            var size = Marshal.ReadInt32(listPtr + FieldOffset(listClass, "_size"));
            if (size == 0) return true;
            if (array == IntPtr.Zero || size < 0 || size > IL2CPP.il2cpp_array_length(array))
            {
                error = "size " + size + " outside the backing array";
                return false;
            }

            var arrayClass = IL2CPP.il2cpp_object_get_class(array);
            var element = IL2CPP.il2cpp_class_get_element_class(arrayClass);
            var stride = IL2CPP.il2cpp_array_element_size(arrayClass);
            var keyOffset = ElementOffset(FieldOffset(element, "Item1"), IntPtr.Size, stride);
            var countOffset = ElementOffset(FieldOffset(element, "Item2"), sizeof(int), stride);
            if (keyOffset < 0 || countOffset < 0)
            {
                error = "unexpected tuple layout stride=" + stride;
                return false;
            }

            // Il2CppArray: object header, bounds pointer and length precede the data.
            var first = array + 4 * IntPtr.Size;
            for (var i = 0; i < size; i++)
            {
                var entry = first + i * stride;
                var keyPtr = Marshal.ReadIntPtr(entry + keyOffset);
                var key = keyPtr == IntPtr.Zero ? null : IL2CPP.Il2CppStringToManaged(keyPtr);
                items.Add((key, Marshal.ReadInt32(entry + countOffset)));
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name + ": " + ex.Message;
            return false;
        }
    }

    // IL2CPP reports value-type field offsets as if the struct were boxed, that
    // is including the object header. Accept either form, but only in bounds.
    private static int ElementOffset(int reported, int fieldSize, int stride)
    {
        var unboxed = reported - 2 * IntPtr.Size;
        if (unboxed >= 0 && unboxed + fieldSize <= stride) return unboxed;
        if (reported >= 0 && reported + fieldSize <= stride) return reported;
        return -1;
    }

    private static int FieldOffset(IntPtr klass, string name)
    {
        var field = IL2CPP.il2cpp_class_get_field_from_name(klass, name);
        if (field == IntPtr.Zero) throw new MissingFieldException(name);
        return (int)IL2CPP.il2cpp_field_get_offset(field);
    }
}
