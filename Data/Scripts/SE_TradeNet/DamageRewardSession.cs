using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRageMath;
using VRage;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Game.Components;
using VRage.Game;
using VRage.Game.ModAPI;
using System.Collections.Concurrent;

namespace SE_TradeNet
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    class DamageRewardSession : MySessionComponentBase
    {
        MyObjectBuilder_SessionComponent m_objectBuilder;
        private ConcurrentDictionary<long, Logic> m_cachedBlocks = new ConcurrentDictionary<long, Logic>();
        bool m_init = false;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            init();
            m_objectBuilder = sessionComponent;
        }


        void init()
        {
            ShowDebugMessage("DamageRewardSession: Registering Damage Handler");
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, OnEntityDamaged);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(5756, MessageHandler);
            m_init = true;
        }

        public override void LoadData()
        {
            ShowDebugMessage("DamageRewardSession: LoadData called");
            if (MyAPIGateway.Session == null || !MyAPIGateway.Session.IsServer)
            {
                ShowDebugMessage("DamageRewardSession: Not a server or session is null");
                return;
            }

            ShowDebugMessage("DamageRewardSession: Server session loaded...");
        }

        private bool IsSupportedBlock(IMyCubeBlock block)
        {
            string subtype = block.BlockDefinition.SubtypeName;
            ShowDebugMessage($"DamageRewardSession: Checking supported block, BlockDefinition: {subtype ?? "null"}");

            if (subtype != "SmallBlockBeacon" && subtype != "LargeBlockBeacon" && subtype != "SmallCargoContainer" && subtype != "LargeCargoContainer" && subtype != "SmallGatlingTurret" && subtype != "LargeGatlingTurret" && subtype != "SmallMissileLauncher" && subtype != "LargeMissileLauncher" && subtype != "SmallAIModule" && subtype != "LargeAIModule")
            {
                ShowDebugMessage($"DamageRewardSession: Block type {subtype} is not supported.");
                return false;
            }

            ShowDebugMessage($"DamageRewardSession: Block type {subtype} is supported.");
            return true;
        }

        private void OnEntityDamaged(object target, ref MyDamageInformation info)
        {
            IMySlimBlock slimBlock = target as IMySlimBlock;
            IMyCubeBlock cubeBlock = slimBlock?.FatBlock;

            try
            {
                if (target == null || slimBlock == null || cubeBlock == null)
                {
                    return;
                }

                ShowDebugMessage($"DamageRewardSession: 손상된 엔티티 {slimBlock.CubeGrid.EntityId}, 지원되는 블록인지 확인 중...");
                if (!IsSupportedBlock(cubeBlock))
                {
                    return;
                }
                ShowDebugMessage($"DamageRewardSession: 손상된 엔티티 {slimBlock.CubeGrid.EntityId}, 지원되는 블록 확인됨");
                // 블록이 캐시되지 않았고 소유자 정보가 있는 경우
                if (!m_cachedBlocks.ContainsKey(slimBlock.CubeGrid.EntityId))
                {
                    ShowDebugMessage($"DamageRewardSession: 엔티티 {slimBlock.CubeGrid.EntityId}가 캐시되지 않음, 소유자 확인 중...");

                    long ownerId = cubeBlock.OwnerId;
                    ShowDebugMessage($"DamageRewardSession: 블록 소유자 ID: {ownerId}");

                    if (ownerId != 0)
                    {
                        IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerId);
                        ShowDebugMessage($"DamageRewardSession: 진영 찾음: {(faction != null ? faction.Tag : "null")}, IsEveryoneNpc: {faction?.IsEveryoneNpc()}");

                        if (faction == null || !faction.IsEveryoneNpc())
                        {
                            ShowDebugMessage("DamageRewardSession: 유효하지 않은 진영, 처리 건너뜀");
                            return;
                        }

                        m_cachedBlocks.TryAdd(slimBlock.CubeGrid.EntityId, ((IMyCubeBlock)cubeBlock).GameLogic.GetAs<Logic>());
                        ShowDebugMessage($"DamageRewardSession: 캐시에 블록 추가됨, EntityId: {slimBlock.CubeGrid.EntityId}");
                    }
                }
                else
                {
                    long ownerId = cubeBlock.OwnerId;
                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerId);
                    ShowDebugMessage($"DamageRewardSession: 캐시에 포함된 진영 찾음: {(faction != null ? faction.Tag : "null")}, IsEveryoneNpc: {faction?.IsEveryoneNpc()}");
                    if (faction == null || !faction.IsEveryoneNpc())
                    {
                        ShowDebugMessage("DamageRewardSession: 유효하지 않은 진영, 처리 건너뜀");
                        return;
                    }
                    ShowDebugMessage($"DamageRewardSession: 유효 EntityId: {slimBlock.CubeGrid.EntityId}");
                }

                long attackerownerId = 0;
                IMyEntity attackerEntity = MyAPIGateway.Entities.GetEntityById(info.AttackerId);
                IMyCubeGrid attackerGrid = attackerEntity as IMyCubeGrid;
                IMyCubeBlock cubeblock = attackerEntity as IMyCubeBlock;
                if (attackerGrid == null && cubeblock != null)
                {
                    ShowDebugMessage($"Not grid {attackerEntity.GetType()} ");
                    attackerGrid = cubeblock.CubeGrid;
                }
                else if (attackerGrid == null )
                {
                    ShowDebugMessage($"Not grid or block {attackerEntity.GetType()} ");
                    IMyGunBaseUser gunplayer = attackerEntity as IMyGunBaseUser;
                    ShowDebugMessage($"Gun fire {gunplayer.OwnerId} ");
                    attackerownerId = gunplayer.OwnerId;
                }
                if (attackerGrid != null )
                {
                    attackerownerId = attackerGrid.BigOwners.Count > 0 ? attackerGrid.BigOwners[0] : 0;
                }
                

                IMyPlayer attackPlayer = null;
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);

                foreach (var player in players)
                {
                    if (player.IdentityId == attackerownerId)
                    {
                        attackPlayer = player;
                        break;
                    }
                }

                IMyFaction attackerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(attackerownerId);
                if(attackPlayer.SteamUserId != null)
                {
                    ShowDebugMessage($"DamageRewardSession: 공격자 엔티티 찾음, {attackerEntity.DisplayName}, 타입: {attackerEntity.GetType()} 소유자 {attackPlayer.SteamUserId}{attackPlayer.DisplayName}{attackerFaction.Tag}");

                }

            }
            catch (Exception _exception)
            {
                ShowDebugMessage($"DamageRewardSession: 예외 발생: {_exception.Message}\n스택 추적: {_exception.StackTrace}");
            }
        }
        private void MessageHandler(ushort channel, byte[] message, ulong recipient, bool reliable)
        {
			long ID = BitConverter.ToInt64(message, 0);
			int value1 = BitConverter.ToInt32(message, 8);
            int value2 = BitConverter.ToInt32(message, 12);
            int value3 = BitConverter.ToInt32(message, 16);
            
			if(!MyAPIGateway.Multiplayer.IsServer)
			{
				
			}
        }
        protected override void UnloadData()
        {
            ShowDebugMessage("DamageRewardSession: UnloadData called");
        }

        // 채팅창에 디버그 메시지를 출력하는 메서드
        private void ShowDebugMessage(string message)
        {
            MyAPIGateway.Utilities.ShowMessage("DamageRewardSession", message); // 메시지를 채팅창에 출력
        }
    }
}
