using System;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;

namespace EfScaffoldTool
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var apiAsm = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lis.Api.dll"));
                var configType = apiAsm.GetType("Lis.Api.Migrations.Configuration", throwOnError: true);
                var config = (DbMigrationsConfiguration)Activator.CreateInstance(configType, nonPublic: true);

                var scaffolder = new MigrationScaffolder(config);
                var migration = scaffolder.Scaffold("AddRoleMenuPermission");

                var outDir = args != null && args.Length > 0
                    ? args[0]
                    : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\web\Lis.Api\Migrations"));

                Directory.CreateDirectory(outDir);

                var designerPath = Path.Combine(outDir, "202607111930000_AddRoleMenuPermission.Designer.cs");
                var resxPath = Path.Combine(outDir, "202607111930000_AddRoleMenuPermission.resx");

                var designer = migration.DesignerCode;
                designer = Regex.Replace(
                    designer,
                    @"return\s+""\d{14}_AddRoleMenuPermission"";",
                    @"return ""202607111930000_AddRoleMenuPermission"";");
                designer = Regex.Replace(
                    designer,
                    @"Id\s*=>\s*""\d{14}_AddRoleMenuPermission"";",
                    @"Id => ""202607111930000_AddRoleMenuPermission"";");

                File.WriteAllText(designerPath, designer);
                WriteResx(resxPath, migration.Resources);

                Console.WriteLine("OK Designer=" + designerPath);
                Console.WriteLine("OK Resx=" + resxPath);

                var preview = Path.Combine(outDir, "_scaffold_preview_AddRoleMenuPermission.Up.txt");
                File.WriteAllText(preview, migration.UserCode ?? "");
                Console.WriteLine("Preview Up written to " + preview);
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
