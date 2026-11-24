using Newtonsoft.Json;

namespace Mod.ModHelper.CommandMod.Hotkey
{
    public class HotkeyCommand : BaseCommand
    {
        public char key;
        public string fullCommand;

        [JsonIgnore]
        public object[] parameters;

        public void execute()
        {
            try
            {
                method.Invoke(null, parameters);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[HotkeyCommand] Error invoking {fullCommand}: {ex}");
            }
        }
    }
}