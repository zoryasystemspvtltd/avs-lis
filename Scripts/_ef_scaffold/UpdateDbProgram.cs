using System;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EfUpdateDb
{
    /// <summary>
    /// Applies pending EF6 migrations for a given Configuration type.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args == null || args.Length < 2)
                {
                    Console.WriteLine("Usage: EfUpdateDb.exe <assembly.dll> <ConfigurationFullTypeName>");
                    return 2;
                }

                var asmPath = Path.GetFullPath(args[0]);
                var configTypeName = args[1];
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
                var migrator = new DbMigrator(config);

                Console.WriteLine("Local migrations:");
                foreach (var id in migrator.GetLocalMigrations())
                {
                    Console.WriteLine("  LOCAL  " + id);
                }

                Console.WriteLine("Database migrations:");
                foreach (var id in migrator.GetDatabaseMigrations())
                {
                    Console.WriteLine("  DB     " + id);
                }

                var pending = migrator.GetPendingMigrations().ToList();
                Console.WriteLine("Pending: " + pending.Count);
                foreach (var id in pending)
                {
                    Console.WriteLine("  PENDING " + id);
                }

                if (pending.Count > 0)
                {
                    migrator.Update();
                    Console.WriteLine("Update-Database completed.");
                }
                else
                {
                    Console.WriteLine("Database is up to date.");
                }

                // Detect pending model changes (would require new migration)
                try
                {
                    var scaffolder = new System.Data.Entity.Migrations.Design.MigrationScaffolder(config);
                    var probe = scaffolder.Scaffold("ProbePendingModelChanges");
                    var hasOps = !string.IsNullOrWhiteSpace(probe.UserCode)
                        && probe.UserCode.IndexOf("CreateTable", StringComparison.OrdinalIgnoreCase) >= 0
                        || probe.UserCode.IndexOf("AddColumn", StringComparison.OrdinalIgnoreCase) >= 0
                        || probe.UserCode.IndexOf("AlterColumn", StringComparison.OrdinalIgnoreCase) >= 0
                        || probe.UserCode.IndexOf("DropTable", StringComparison.OrdinalIgnoreCase) >= 0
                        || probe.UserCode.IndexOf("DropColumn", StringComparison.OrdinalIgnoreCase) >= 0
                        || probe.UserCode.IndexOf("CreateIndex", StringComparison.OrdinalIgnoreCase) >= 0
                        || probe.UserCode.IndexOf("AddForeignKey", StringComparison.OrdinalIgnoreCase) >= 0;

                    // Empty Up() still has class boilerplate
                    var emptyish = probe.UserCode != null
                        && !probe.UserCode.Contains("CreateTable")
                        && !probe.UserCode.Contains("AddColumn")
                        && !probe.UserCode.Contains("AlterColumn")
                        && !probe.UserCode.Contains("DropTable")
                        && !probe.UserCode.Contains("DropColumn")
                        && !probe.UserCode.Contains("Rename")
                        && !probe.UserCode.Contains("Sql(");

                    Console.WriteLine(emptyish
                        ? "Model sync: CLEAN (no pending model changes)"
                        : "Model sync: PENDING CHANGES detected — review needed");
                    if (!emptyish)
                    {
                        var preview = Path.Combine(Path.GetDirectoryName(asmPath), "_pending_model_probe.txt");
                        File.WriteAllText(preview, probe.UserCode ?? "");
                        Console.WriteLine("Probe written: " + preview);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Model probe warning: " + ex.Message);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }
    }
}
