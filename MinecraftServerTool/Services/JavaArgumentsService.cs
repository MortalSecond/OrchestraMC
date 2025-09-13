using System.IO;

namespace MinecraftServerTool.Services
{
    public class JavaArgumentsService
    {
        public void SaveJvmArgs(string modpackPath, int ramGb)
        {
            string filePath = Path.Combine(modpackPath, "user_jvm_args.txt");
            string args = $"-Xmx{ramGb}G -Xms{ramGb}G";

            File.WriteAllText(filePath, args);
        }
    }
}
