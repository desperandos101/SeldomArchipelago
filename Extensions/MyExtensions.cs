using System;
using System.Linq;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Terraria.ModLoader.IO;
using Terraria.ModLoader.Config;
using SeldomArchipelago.Locking;
using MonoMod.Cil;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MyExtensions {
    public static class MyExtensions {
        static Random rnd = new();
        public static (T[], T[]) SplitArray<T>(this T[] array, int index) =>
        (array.Take(index).ToArray(), array.Skip(index).ToArray());

        public static bool ContainsDuplicates<T>(this IEnumerable<T> theEnum) {
        HashSet<T> theSet = theEnum.ToHashSet();
            if (theSet.Count() == theEnum.Count())
                return false;
            return true;
        }
        public static T[] GetRandomSubset<T>(this IEnumerable<T> theEnum, int newArrayCount, bool removeSubsetFromList = false) {
            List<T> oldList = theEnum.ToList();
            if (oldList.Count() <= newArrayCount || newArrayCount == -1) {
                if (removeSubsetFromList && theEnum is List<T> theList)
                    theList.RemoveAll(t => true);
                return oldList.ToArray();
            }
            T[] newArray = new T[newArrayCount];
            for (int i = 0; i < newArrayCount; i++) {
                T randItem = oldList[rnd.Next(oldList.Count())];
                newArray[i] = randItem;
                oldList.Remove(randItem);
                if (removeSubsetFromList && theEnum is List<T> theList) {
                    theList.Remove(randItem);
                } else if (removeSubsetFromList && theEnum is HashSet<T> theSet) {
                    theSet.Remove(randItem);
                } else if (removeSubsetFromList && theEnum is T[]) {
                    throw new Exception("GetRandomSubset can't remove elements from an array.");
                } else if (removeSubsetFromList) {
                    throw new Exception("GetRandomSubset can't remove elements from something that isn't a list or set.");
                }
            }
            return newArray;
        }
        public static Dictionary<TKey, TValue> ConvertToDict<TKey, TValue>(this (TValue, TKey[])[] tupleArray) {
            Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
            foreach((TValue, TKey[]) tuple in tupleArray) {
                foreach (TKey key in tuple.Item2) {
                    dict[key] = tuple.Item1;
                }
            }
            return dict;
        }
        public static TValue? UseAsDict<TKey, TValue>(this (TValue, TKey[])[] tupleDict, TKey value, bool refuseZero = false) where TValue : struct
        {
            foreach((TValue, TKey[]) idSet in tupleDict) {
                if(idSet.Item2.Contains(value)) {
                    return idSet.Item1;
                }
            }
            if (refuseZero)
            {
                throw new Exception($"Value {value} not found in tupledict.");
            }
            return null;
        }
        public static string UseAsDict<TKey>(this (string, TKey[])[] tupleDict, TKey value, out TKey[] array)
        {
            array = null;
            foreach ((string, TKey[]) idSet in tupleDict)
            {
                if (idSet.Item2.Contains(value))
                {
                    array = idSet.Item2;
                    return idSet.Item1;
                }
            }
            return null;
        }
        public static string UseAsDict<TKey>(this (string, TKey[])[] tupleDict, TKey value) => UseAsDict<TKey>(tupleDict, value, out var _);
        public static void SaveDict<TKey, TValue>(this TagCompound tag, Dictionary<TKey, TValue> dict, string dictName)
        {
            tag[$"{dictName}Keys"] = dict.Keys.ToList();
            tag[$"{dictName}Values"] = dict.Values.ToList();
        }
        public static void LoadDict<TKey, TValue>(this TagCompound tag, Dictionary<TKey, TValue> dict, string dictName)
        {
            if (tag.ContainsKey($"{dictName}Keys"))
            {
                List<TKey> dictKeys = tag.Get<List<TKey>>($"{dictName}Keys");
                List<TValue> dictValues = tag.Get<List<TValue>>($"{dictName}Values");
                for (int i = 0; i < dictKeys.Count; i++)
                {
                    dict[dictKeys[i]] = dictValues[i];
                }
            }
        }
        public static void OverrideField(this ILCursor cursor, FieldInfo field, Func<bool> flag)
        {
            cursor.GotoNext(i => i.MatchLdsfld(field));
            cursor.Index++;
            cursor.EmitPop();
            cursor.EmitDelegate(() =>
            {
                return flag() ? 1 : 0;
            });
        }
    }
}