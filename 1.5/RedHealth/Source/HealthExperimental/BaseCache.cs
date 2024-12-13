//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RedHealth
//{
//    public interface ICacheable
//    {
//        bool RegenerateCache();
//    }

//    public abstract class DictCache<T, V> where V : ICacheable
//    {
//        public static ConcurrentDictionary<T, V> Cache { get; set; } = new ConcurrentDictionary<T, V>();

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="key"></param>
//        /// <param name="forceRefresh"></param>
//        /// <param name="canRegenerate">The Cache will not be regenerated, if one does not exist it will simply return default values.</param>
//        /// <returns></returns>
//        public static V GetCache(T key, out bool newEntry, bool forceRefresh = false, bool canRegenerate = true)
//        {
//            newEntry = false;
//            if (key == null)
//                return default;
//            if (Cache.TryGetValue(key, out V data))
//            {
//                // Check if the cache has timed out
//                if (forceRefresh)
//                {
//                    data.RegenerateCache();
//                    return data;
//                }
//                else
//                {
//                    return data;
//                }
//            }
//            else
//            {
//                newEntry = true;
//                V newData = (V)Activator.CreateInstance(typeof(V), key);
//                if (canRegenerate)
//                {
//                    newData.RegenerateCache();
//                }
//                Cache[key] = newData;
//                return newData;
//            }
//        }
//    }
//}
