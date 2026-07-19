using System;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;

namespace EfScaffoldTool
{
    /// <summary>
    /// Scaffolds an EF6 migration (UserCode preview + Designer.cs + .resx) for a given
    /// DbMigrationsConfiguration. Usage:
    ///   EfScaffoldTool.exe &lt;assembly.dll&gt; &lt;ConfigurationFullTypeName&gt; &lt;MigrationName&gt; &lt;outDir&gt; [fixedTimestamp]
    /// If fixedTimestamp (e.g. 202607111930000) is provided, the migration id and file names use it
    /// instead of the scaffolder-generated timestamp.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args == null || args.Length < 4)
                {
                    Console.WriteLine("Usage: EfScaffoldTool.exe <assembly.dll> <ConfigurationFullTypeName> <MigrationName> <outDir> [fixedTimestamp]");
                    return 2;
                }

                var asmPath = Path.GetFullPath(args[0]);
                var configTypeName = args[1];
                var migrationName = args[2];
                var outDir = Path.GetFullPath(args[3]);
                var fixedTimestamp = args.Length > 4 ? args[4] : null;

                AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                {
                    var name = new AssemblyName(e.Name).Name + ".dll";
                    var candidate = Path.Combine(Path.GetDirectoryName(asmPath), name);
                    if (File.Exists(candidate))
                    {
                        return Assembly.LoadFrom(candidate);
                    }
                    return null;
                };

                var asm = Assembly.LoadFrom(asmPath);
                var configType = asm.GetType(configTypeName, throwOnError: true);
                var config = (DbMigrationsConfiguration)Activator.CreateInstance(configType, nonPublic: true);

                var scaffolder = new MigrationScaffolder(config);
                var migration = scaffolder.Scaffold(migrationName);

                var migrationId = migration.MigrationId;
                if (!string.IsNullOrEmpty(fixedTimestamp))
                {
                    var desired = fixedTimestamp + "_" + migrationName;
                    migration.DesignerCode = Regex.Replace(
                        migration.DesignerCode,
                        @"""\d+_" + Regex.Escape(migrationName) + @"""",
                        "\"" + desired + "\"");
                    migrationId = desired;
                }

                Directory.CreateDirectory(outDir);

                var designerPath = Path.Combine(outDir, migrationId + ".Designer.cs");
                var resxPath = Path.Combine(outDir, migrationId + ".resx");
                var userCodePath = Path.Combine(outDir, "_scaffold_preview_" + migrationName + ".Up.txt");

                File.WriteAllText(designerPath, migration.DesignerCode);
                WriteResx(resxPath, migration.Resources);
                File.WriteAllText(userCodePath, migration.UserCode ?? "");

                Console.WriteLine("OK Id=" + migrationId);
                Console.WriteLine("OK Designer=" + designerPath);
                Console.WriteLine("OK Resx=" + resxPath);
                Console.WriteLine("Preview Up written to " + userCodePath);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void WriteResx(string path, System.Collections.Generic.IDictionary<string, object> resources)
        {
            using (var writer = new ResXResourceWriter(path))
            {
                foreach (var kv in resources)
                {
                    writer.AddResource(kv.Key, kv.Value);
                }
            }
        }
    }
}
