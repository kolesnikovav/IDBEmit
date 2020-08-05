using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IDBEmit
{
    public class DBEmitService<T> where T:DbContext
    {
        /// <summary>
        /// root path for created types folder
        /// </summary>
        private string _rootPath;
        /// <summary>
        /// IndexedDB database name
        /// </summary>
        private string _storageName;
        /// <summary>
        /// referenced enums
        /// </summary>
        private readonly List<Type> _enumTypes = new List<Type>();
        /// <summary>
        /// keep references for key Type
        /// </summary>
        private readonly Dictionary<Type, List<Type>> _references = new Dictionary<Type, List<Type>>();
        /// <summary>
        /// entity types, that should be present at client side (indexeddb)
        /// </summary>
        private readonly Dictionary<Type, ClientStorageAttribute> _injectedTypes = new Dictionary<Type, ClientStorageAttribute>();
        /// <summary>
        /// prepared import typescript directives for entity types and enums
        /// </summary>
        private readonly Dictionary<Type, string> _importDirectives = new Dictionary<Type, string>();
        /// <summary>
        /// key fields of entities
        /// </summary>
        private readonly Dictionary<Type, Tuple<string,Type>> _entityKeys = new Dictionary<Type, Tuple<string,Type>>();
        private static readonly Type[] _numericTypes = new Type[]
        {
            typeof(byte), typeof(sbyte) , typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(decimal), typeof(float)
        };
        private static  Type ClientStorageAttributeType = typeof(ClientStorageAttribute);
        private static Type KeyAttributeType = typeof(KeyAttribute);

        private void EnsurePathExists(string path)
        {
            if (!Directory.Exists(path)) throw(new DirectoryNotFoundException("Directory "+ path + " not exists"));
        }
        private void ClearRootFolder(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception e)
            {
                throw(new IOException("Directory "+ path + " cannot be deleted \n More details: \n" + e.Message));
            }
        }
        private void CreateFolder(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                throw(new IOException("Cannot create directory "+ path + " \n More details: \n" + e.Message));
            }
        }
        /// <summary>
        /// returns type/java script type from property type
        /// </summary>
        /// <param name="propertyType">Type to be converted</param>
        private string ConvertToTsType(Type propertyType)
        {
            if (propertyType == typeof(bool)) return "boolean";
            if (_numericTypes.Contains(propertyType)) return "number";
            if (propertyType == typeof(Guid)) return "string";
            if (propertyType.IsEnum)
            {
                if (!_enumTypes.Contains(propertyType)) _enumTypes.Add(propertyType);
                return Utils.NormalizedName(propertyType.ToString());
            }
            else if (propertyType.IsClass && _entityKeys.ContainsKey(propertyType))
            {
                // add reference type or key of reference
                if ( _entityKeys[propertyType] != null)
                {
                    return Utils.NormalizedName(propertyType.ToString()) + "|" + ConvertToTsType(_entityKeys[propertyType].Item2);
                }
                else
                {
                    return propertyType.ToString();
                }
             }
            return "string";
        }
        private string ScaffoldTypeToTypescript(Type entityType)
        {
            string t ="\t";
            string res = "";
            var localRefs = _references[entityType];
            foreach (var r in localRefs)
            {
                res += _importDirectives[r] + "\n";
            }
            res += "export class "+Utils.NormalizedName(entityType.ToString())+" {\n";
            foreach(var p in entityType.GetProperties())
            {
                bool isKey = _entityKeys.ContainsKey(p.PropertyType) && _entityKeys[p.PropertyType].Item1 == p.Name;
                res += t + p.Name +  (isKey ? "" : "?") + " : " + ConvertToTsType(p.PropertyType) + ";\n";
            }
            res += "}\n";
            return res;
        }
        private string ScaffoldEnums(Type enumType)
        {
            string t ="\t";
            string res = "export enum "+Utils.NormalizedName(enumType.ToString())+" {\n";
            foreach(var p in enumType.GetEnumValues())
            {
                res += t + Utils.NormalizedName(p.ToString()) + ",\n";
            }
            res = res.Substring(0,res.Length - 2) + "\n";
            res += "}\n";
            return res;
        }
        private string ScaffoldDatabase()
        {
            string t ="\t";
            string res = "export function createDB (db, dbReq) => {\n" +
            "";
            // foreach(var p in enumType.GetEnumValues())
            // {
            //     res += t + p.ToString() + ";\n";
            // }
            // res += "}\n";
            return res;
        }
        /// <summary>
        /// Discover DB context and fill internal datasets
        /// </summary>
        private void GetEntityKeys()
        {
            Type dSet = typeof(DbSet<>);
            foreach (var p in typeof(T).GetProperties())
            {
                if (p.PropertyType.IsGenericType)
                {
                    Type entityType = p.PropertyType.GenericTypeArguments[0];
                    ClientStorageAttribute c = entityType.GetCustomAttribute(ClientStorageAttributeType) as ClientStorageAttribute;
                    _importDirectives.Add(entityType, "import { " + Utils.NormalizedName(entityType.ToString()) + " } from './" +
                            Path.Combine("types", Utils.NormalizedName(entityType.ToString()) + "'"));
                    if (c != null)
                    {
                        _injectedTypes.Add(entityType, c);
                    }
                    foreach (var pE in entityType.GetProperties())
                    {
                        KeyAttribute k = pE.GetCustomAttribute(KeyAttributeType) as KeyAttribute;
                        if (k != null)
                        {
                            _entityKeys.Add(entityType, new Tuple<string, Type>(pE.Name, pE.PropertyType));
                            break;
                        }
                    }
                    if (!_entityKeys.ContainsKey(entityType))
                    {
                        _entityKeys.Add(entityType, null);
                    }
                }
            }
            foreach (var entity in _entityKeys)
            {
                List<Type> localRefs = new List<Type>();
                foreach (var pE in entity.Key.GetProperties())
                {
                    if (pE.PropertyType.IsEnum || _entityKeys.ContainsKey(pE.PropertyType))
                    {
                        if (!localRefs.Contains(pE.PropertyType))
                        {
                            localRefs.Add(pE.PropertyType);
                        }
                        if (!_importDirectives.ContainsKey(pE.PropertyType) && pE.PropertyType.IsEnum)
                        {
                            _importDirectives.Add(pE.PropertyType, "import { " + Utils.NormalizedName(pE.PropertyType.ToString()) + " } from '../" +
                                Path.Combine("enums", Utils.NormalizedName(pE.PropertyType.ToString()) + "'"));
                        }
                    }
                }
                _references.Add(entity.Key, localRefs);
            }
        }
        /// <summary>
        /// Initialize instance for DBContext and scaffold this to typescript
        /// </summary>
        /// <param name="rootPath">root path for created types folder. Should be in client app source folder and included in client app and including into bundling toolchain</param>
        /// <param name="storageName">IndexedDB database name </param>
        public void Initialize(string rootPath, string storageName)
        {
            _rootPath = Path.Combine(rootPath,Utils.NormalizedName(typeof(T).ToString()));
            _storageName = storageName;
            GetEntityKeys();
            EnsurePathExists(rootPath);
            if (!Directory.Exists(_rootPath))
            {
                CreateFolder(_rootPath);
            }
            else
            {
                ClearRootFolder(_rootPath);
                CreateFolder(_rootPath);
            }
            CreateFolder(Path.Combine(_rootPath,"types"));
            CreateFolder(Path.Combine(_rootPath,"enums"));
            foreach( var q in _entityKeys)
            {
                string s = ScaffoldTypeToTypescript(q.Key);
                using (System.IO.StreamWriter file =
                       new System.IO.StreamWriter(Path.Combine(_rootPath,"types", Utils.NormalizedName(q.Key.ToString()) + ".ts"), true))
                {
                    file.Write(s);
                }
            }
            foreach( var q in _importDirectives.Where(v => v.Key.IsEnum))
            {
                string s = ScaffoldEnums(q.Key);
                using (System.IO.StreamWriter file =
                       new System.IO.StreamWriter(Path.Combine(_rootPath,"enums", Utils.NormalizedName(q.Key.ToString()) + ".ts"), true))
                {
                    file.Write(s);
                }
            }
            // create IndexedDB proection of inected entities
            foreach( var q in _injectedTypes)
            {
                // string s = ScaffoldEnums(q.Key);
                // using (System.IO.StreamWriter file =
                //        new System.IO.StreamWriter(Path.Combine(_rootPath,"enums", q.Key.ToString() + ".ts"), true))
                // {
                //     file.Write(s);
                // }
            }
        }
    }
}