﻿//This file is from https://github.com/SanchozzDeponianin/ONIMods 
//author's lisence

//MIT License

//Copyright (c) 2020 SanchozzDeponianin

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.


using System;
using System.Collections.Generic;
using HarmonyLib;
using PeterHan.PLib.Core;

namespace TeleportSuitMod.SanchozzONIMods.Lib
{
    public sealed class KAnimGroupManager : PForwardedComponent
    {
        // хеши названий некоторых аним групп. сами названия хз куда заныканы
        private const int SWAPS = -77805842;
        private const int INTERACTS = -1371425853;

        private static readonly Version VERSION = new Version(1, 0, 0, 0);
        internal static KAnimGroupManager Instance { get; private set; }

        private readonly Dictionary<HashedString, HashSet<string>> anims_table;
        public KAnimGroupManager()
        {
            anims_table = new Dictionary<HashedString, HashSet<string>>(1);
        }

        public override Version Version => VERSION;

        public void RegisterInteractAnims(params string[] anims)
        {
            RegisterAnims(new HashedString(INTERACTS), anims);
        }

        public void RegisterSwapAnims(params string[] anims)
        {
            RegisterAnims(new HashedString(SWAPS), anims);
        }

        public void RegisterAnims(HashedString target_group, params string[] anims)
        {
            if (!target_group.IsValid)
                throw new ArgumentNullException(nameof(target_group));
            if (anims == null)
                throw new ArgumentNullException(nameof(anims));
            RegisterForForwarding();
            if (!anims_table.ContainsKey(target_group))
                anims_table[target_group] = new HashSet<string>();
            foreach (var anim_name in anims)
            {
                if (anims_table[target_group].Add(anim_name))
                {
#if DEBUG
                    PUtil.LogDebug($"Registered anim '{anim_name}' to add into group '{target_group}'.");
#endif
                }
                else
                {
                    PUtil.LogWarning($"Anim '{anim_name}' already registered to add into group '{target_group}', skip.");
                }
            }
        }

        public override void Initialize(Harmony plibInstance)
        {
            Instance = this;
            plibInstance.Patch(typeof(KAnimGroupFile), nameof(KAnimGroupFile.LoadAll), prefix: PatchMethod(nameof(KAnimGroupFile_LoadAll_Prefix)));
        }

        private static void KAnimGroupFile_LoadAll_Prefix()
        {
            Instance?.InvokeAllProcess(0, null);
        }

        public override void Process(uint operation, object args)
        {
            if (operation == 0)
                ProcessAnims();
        }

        private void ProcessAnims()
        {
            var groups = KAnimGroupFile.GetGroupFile().GetData();
            foreach (var group_id in anims_table.Keys)
            {
                var targetGroup = KAnimGroupFile.GetGroup(group_id);
                if (targetGroup == null)
                {
                    PUtil.LogWarning($"Anim group '{group_id}' not found.");
                }
                else
                {
                    foreach (var anim_name in anims_table[group_id])
                    {
                        if (!Assets.TryGetAnim(anim_name, out var kanim))
                        {
                            PUtil.LogWarning($"Missing Anim '{anim_name}'.");
                        }
                        else
                        {
                            if (kanim.mod == null)
                            {
                                PUtil.LogWarning($"Anim '{anim_name}' probably is not modded anim, skip.");
                            }
                            else
                            {
                                // удаляем группу, созданную игрой автоматически во время загрузки моддовой анимации
                                groups.RemoveAll(g => (g.animNames.Count == 1 && g.animNames[0] == kanim.name));

                                if (!targetGroup.animNames.Contains(kanim.name))
                                {
                                    targetGroup.animNames.Add(kanim.name);
                                }
                                if (!targetGroup.animFiles.Contains(kanim))
                                {
                                    targetGroup.animFiles.Add(kanim);
#if DEBUG
                                    PUtil.LogDebug($"Added anim '{anim_name}' into group '{group_id}'.");
#endif
                                }
                                else
                                {
                                    PUtil.LogDebug($"Anim '{anim_name}' is already in group '{group_id}'! Are two mods adding anims with the same name ?");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}