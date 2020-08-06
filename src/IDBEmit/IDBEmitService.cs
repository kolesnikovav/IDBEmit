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
        /// keep client-side indexes for injected types
        /// </summary>
        private readonly Dictionary<Type, Dictionary<string,bool>> _indexes = new Dictionary<Type, Dictionary<string,bool>>();
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
        private static  Type ClientIndexAttributeType = typeof(ClientIndexAttribute);
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
            string res = "type cIndex = {\n" +
                         "\t name: string," +
                         "\t path: string," +
                         "\t unique: boolean\n" +
                         "}\n\n"; // type ts for index
            // create object store with indexes
            res += "const createStore = (db: IDBDatabase, name: string, key: string, indexes?: cIndex[]) => {\n"+
                   "\t let a;\n" +
                   "\t if (!db.objectStoreNames.contains(name)) {\n" +
                   "\t       db.createObjectStore(name, { keyPath: key, autoincrement: false})\n" +
                   "\t } else {\n" +
                   "\t    a = dbReq.transaction.objectStore(name); \n" +
                   "\t }\n" +
                   "\t indexes.map(v => {\n" +
                   "\t   if (!a.indexNames.contains(v.name)){\n" +
                   "\t a.createIndex(v.name, v.path, {unique: v.unique}))\n" +
                   "\t  }"+
                   "}\n\n";
            res += "const datasets = [\n";
            string res1 ="";
            foreach(var p in _injectedTypes)
            {
                res1 += "{ name: " + Utils.NormalizedName(p.Key.ToString()) + ",\n  key: " + _entityKeys[p.Key].Item1;
                if (_indexes.ContainsKey(p.Key))
                {
                    res1 += ",\n  indexes: [\n";
                    foreach(var i in _indexes[p.Key])
                    {
                        res1 += "\t{ name: " + i.Key + ", path: " + i.Key + ", unique: " + i.Value.ToString().ToLowerInvariant() + "}\n" ;

                    }
                    res1 += "]\n";
                }
                res1 += "},\n";
            }
            res1 = res1.Substring(0, res1.Length - 2) + "\n";

            res += res1 + "]\n\n";
            res += "const createDatabase = (db: IDBDatabase) => {\n"+
                   "\t datasets.map(v => createStore(db, v.name, v.key, v.indexes)\n" +
                   "}\n\n";
            res += "export const database = (name: string, version: number): IDBDatabase => {\n"+
                   "\t const dbRequest = indexedDB.open( name, version)\n" +
                   "\t let db = dbReq.result\n" +
                   "\t dbRequest.onupgradeneeded = ( ev ) => {\n" +
                   "\t db = (ev.target as IDBOpenDBRequest).result\n" +
                   "\t   createDatabase((ev.target as IDBOpenDBRequest).result)\n"+
                   "\t}\n" +
                   "\t dbRequest.onsuccess = (ev) => {\n db = (ev.target as IDBOpenDBRequest).result\n}\n" +
                   "\t dbRequest.onerror = (ev) => {}\n" +
                   "\t return db\n" +
                   "}\n\n";
            res +=  "export const addToDatabase = <T>(db: IDBDatabase,message: T|T[]) => {\n"+
                    "\t const storeName = (Array.isArray(message) ? typeof message[0] : typeof message).toString()\n" +
                    "\t  let tx = db.transaction([storeName], 'readwrite')\n" +
                    "\t  let store = tx.objectStore(storeName)\n" +
                    "\t  if (Array.isArray(message)) {\n" +
                    "\t     message.map(v => store.add(v))\n" +
                    "\t } else {\n" +
                    "\t      store.add(message)\n" +
                    "\t  }\n" +
                    "\t  tx.oncomplete = () => {}\n"+
                    "\t  tx.onerror = (event) => {}\n"+
                    "\t}\n";
            return res;

        }
        /// <summary>
        /// Clear internal data when it no nessesary
        /// </summary>
        private void ClearInternalData()
        {
            this._entityKeys.Clear();
            this._enumTypes.Clear();
            this._importDirectives.Clear();
            this._injectedTypes.Clear();
            this._references.Clear();
            this._indexes.Clear();
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
        /// Discover injected types and create client side indexes
        /// </summary>
        private void GetIndexes()
        {
            foreach (var v in _injectedTypes)
            {
                Dictionary<string, bool> d = new Dictionary<string, bool>();
                foreach(var p in v.Key.GetProperties())
                {
                    ClientIndexAttribute k = p.GetCustomAttribute(ClientIndexAttributeType) as ClientIndexAttribute;
                    if (k != null)
                    {
                        d.Add(Utils.NormalizedName(p.Name), k.Unique);
                    }
                }
                if (d.Count>0)
                {
                    _indexes.Add(v.Key,d);
                }
            }
        }
        /// <summary>
        /// Initialize instance for DBContext and scaffold this to typescript
        /// </summary>
        /// <param name="rootPath">root path for created types folder. Should be in client app source folder and included in client app and including it into bundling toolchain</param>
        /// <param name="storageName">IndexedDB database name </param>
        public void Initialize(string rootPath, string storageName)
        {
            _rootPath = Path.Combine(rootPath,Utils.NormalizedName(typeof(T).ToString()));
            _storageName = storageName;
            GetEntityKeys();
            GetIndexes();
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
            string a = ScaffoldDatabase();
            using (System.IO.StreamWriter file =
                   new System.IO.StreamWriter(Path.Combine(_rootPath, "database.ts"), true))
            {
                file.Write(a);
            }
            ClearInternalData();
        }
    }
}