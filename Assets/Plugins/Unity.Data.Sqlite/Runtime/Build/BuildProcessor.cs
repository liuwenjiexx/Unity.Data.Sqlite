#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yanmonet.Data.Sqlite.Editor
{
    class BuildProcessor : IFilterBuildAssemblies
#if UNITY_ANDROID
      , IPostGenerateGradleAndroidProject
#endif
    {
        string[] SqliteAssemblys = new string[] {
            "Yanmonet.Data.Sqlite.dll",
            "Mono.Data.Sqlite.dll",   
            "Mono.Data.SqliteClient.dll" ,
            "sqlite3.dll",
            "libsqlite3.so",
        };

        public int callbackOrder => 0;

        /// <summary>
        /// <paramref name="assemblies"/> 只包含 .dll ，不包含 .so
        /// </summary>
        public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
        {

#if EXCLUDE_DATABASE_SQLITE
            List<string> newAssemblies = new List<string>(assemblies);

            for (int i = newAssemblies.Count - 1; i >= 0; i--)
            {
                string filename = Path.GetFileName(assemblies[i]);

                if (SqliteAssemblys.Any(o => string.Equals(filename, o, StringComparison.InvariantCultureIgnoreCase)))
                {
                    newAssemblies.RemoveAt(i);
                    continue;
                }
            }
            return newAssemblies.ToArray();
#endif
            return assemblies;
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {

#if EXCLUDE_DATABASE_SQLITE
            foreach (var file in Directory.GetFiles(path, "*.so", SearchOption.AllDirectories))
            {
                string filename = Path.GetFileName(file);
                if (SqliteAssemblys.Any(o => string.Equals(filename, o, StringComparison.InvariantCultureIgnoreCase)))
                {
                    File.Delete(file);
                }
            }
#endif
        }

    }
}
#endif