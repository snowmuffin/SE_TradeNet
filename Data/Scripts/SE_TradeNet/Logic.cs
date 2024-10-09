using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Common.Utils;
using VRageMath;
using VRage;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Utils;
using VRage.Game.Entity.UseObject;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Voxels;
using System.Linq;
using System.Linq.Expressions;

namespace SE_TradeNet
{ 
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeBlock))]
    public class Logic : MyGameLogicComponent
    {
        // 컴포넌트 상태를 나타내는 플래그
        bool m_closed = false;
        MyObjectBuilder_EntityBase m_objectBuilder;
        public long m_attackerId;
        bool m_init = false;
        BlockDamageData savemessage = new BlockDamageData();

        // 컴포넌트 초기화 메서드
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            m_objectBuilder = objectBuilder; // 엔티티의 초기 데이터를 저장합니다.
            m_attackerId = 0; // 공격자 ID 초기화
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.BEFORE_NEXT_FRAME; // 매 프레임마다 업데이트 필요 설정
        }

        // 네트워크 균형 업데이트 메서드: 서버에서 네트워크 메시지를 전송합니다.
        public void UpdateNetworkBalanced()
        {
            if (!m_closed && Entity.InScene) // 엔티티가 닫히지 않았고 씬에 존재할 때만 동작
            {
                if (MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Multiplayer.IsServer) // 서버에서만 동작
                {
                    byte[] message = new byte[16];
                    byte[] messageID = BitConverter.GetBytes(Entity.EntityId); // 엔티티 ID를 바이트 배열로 변환
                    byte[] messageValue1 = BitConverter.GetBytes(m_attackerId); // 공격자 ID를 바이트 배열로 변환

                    // 메시지 배열에 엔티티 ID와 공격자 ID 저장
                    for (int i = 0; i < 8; i++) {
                        message[i] = messageID[i];
                    }
                    for (int i = 0; i < 8; i++) {
                        message[i + 8] = messageValue1[i];
                    }

                    // 다른 클라이언트로 메시지 전송
                    MyAPIGateway.Multiplayer.SendMessageToOthers(5859, message, true);
                }
            }
        }

        // 컴포넌트가 종료될 때 호출되는 메서드
        public override void Close()
        {
            m_closed = true; // 컴포넌트가 닫혔음을 표시

            // 터미널 블록으로 캐스팅하여 블록 관련 이벤트 해제 가능
            Sandbox.ModAPI.IMyTerminalBlock terminalBlock = Entity as Sandbox.ModAPI.IMyTerminalBlock;
            //terminalBlock.AppendingCustomInfo -= UpdateBlockInfo;

            // 다른 모듈에서의 업그레이드 정보를 삭제 (임시 주석 처리)
            /*if (Upgradecore.Upgrades.ContainsKey(Entity.EntityId)) {
                Upgradecore.Upgrades.Remove(Entity.EntityId);
            }*/
        }

        // 저장소 초기화 메서드
        private void InitStorage()
        {
            if (Entity.Storage == null)
            {
                Entity.Storage = new MyModStorageComponent(); // 저장소가 없으면 새로 생성
            }
        }

        // 저장소 데이터 로드 메서드
        private void LoadStorage()
        {
            // 저장된 데이터가 없으면 반환
            if (!Entity.Storage.ContainsKey(BlockDamageData.StorageGuid))
                return;

            var data = Entity.Storage.GetValue(BlockDamageData.StorageGuid);
            try
            {
                // 저장된 데이터를 이진 형식에서 역직렬화하여 로드
                var storagedata = MyAPIGateway.Utilities.SerializeFromBinary<BlockDamageData>(Convert.FromBase64String(data));
                m_attackerId = storagedata.attackerId; // 공격자 ID 로드
            }
            catch (Exception e)
            {
                // 저장 데이터가 손상된 경우 복구
                SaveStorage();
            }
        }

        // 저장소 데이터 저장 메서드
        private void SaveStorage()
        {
            if (Entity.Storage == null)
                InitStorage(); // 저장소가 없으면 초기화

            // 저장할 데이터 생성
            var storageData = new BlockDamageData
            {
                attackerId = m_attackerId
            };

            // 데이터를 이진 형식으로 직렬화하여 저장소에 저장
            var data = MyAPIGateway.Utilities.SerializeToBinary(storageData);
            Entity.Storage.SetValue(BlockDamageData.StorageGuid, Convert.ToBase64String(data));
        }

        // 시뮬레이션 업데이트 메서드: 매 프레임마다 호출됩니다.
        public override void UpdateBeforeSimulation()
        {
            IMyCubeBlock cubeBlock = Entity as IMyCubeBlock;
            if (!m_init)
            {
                m_init = true; // 초기화 여부 설정
            }

            if (cubeBlock == null)
                return;

            IMyCubeGrid grid = cubeBlock.CubeGrid;
            if (grid == null)
                return;

            // 피해 데이터를 저장 메시지에 설정
            savemessage.attackerId = m_attackerId;
        }

        // 첫 프레임 이전에 한 번만 호출되는 업데이트 메서드
        public override void UpdateOnceBeforeFrame()
        {
            InitStorage(); // 저장소 초기화
            LoadStorage(); // 저장소 데이터 로드
            SaveStorage(); // 저장소 데이터 저장
        }
    }
}