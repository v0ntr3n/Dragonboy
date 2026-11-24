using System;
using System.Linq;
using System.Reflection;

namespace Mod.ModHelper.CommandMod
{
    internal static class CommandUtils
    {
        const BindingFlags STATIC_VOID =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static |
            BindingFlags.InvokeMethod;

        /// <summary>
        /// Lấy danh sách các hàm trong theo tên của class.
        /// </summary>
        /// <remarks> Lưu ý:
        /// <list type="bullet">
        /// <item><description>Chỉ lấy các hàm public static void.</description></item>
        /// <item><description>Tên class phải bao gồm cả namespace.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="typeFullName"></param>
        /// <returns>Danh sách các hàm trong class.</returns>
        public static MethodInfo[] getMethods(string typeFullName)
        {
            if (string.IsNullOrEmpty(typeFullName))
                return null;
            Type type = typeof(CommandUtils).Assembly
                .GetTypes().FirstOrDefault(x => x.FullName != null && x.FullName.Equals(typeFullName, StringComparison.OrdinalIgnoreCase));
            return type?.GetMethods(STATIC_VOID);
        }

        /// <summary>
        /// Lấy danh sách tất cả các hàm của tệp Assembly-CSharp.dll.
        /// </summary>
        /// <remarks> Lưu ý:
        /// <list type="bullet">
        /// <item><description>Chỉ lấy các hàm public static void.</description></item>
        /// <item><description>Tên class phải bao gồm cả namespace.</description></item>
        /// </list>
        /// </remarks>
        /// <returns>Danh sách các hàm của Assembly hiện tại.</returns>
        private static Type[] GetAssemblyTypesSafe(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null).ToArray();
            }
        }

        public static MethodInfo[] GetMethods()
        {
            return GetAssemblyTypesSafe(typeof(CommandUtils).Assembly)
                .Where(x => x != null && x.IsClass)
                .SelectMany(x =>
                {
                    try { return x.GetMethods(STATIC_VOID); }
                    catch { return Array.Empty<MethodInfo>(); }
                })
                .ToArray();
        }
    }
}
