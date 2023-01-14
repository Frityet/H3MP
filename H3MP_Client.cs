﻿using FistVR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace H3MP
{
    internal class H3MP_Client : MonoBehaviour
    {
        private static H3MP_Client _singleton;
        public static H3MP_Client singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                {
                    _singleton = value;
                }
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(H3MP_Client)} instance already exists, destroying duplicate!");
                    Destroy(value);
                }
            }
        }

        public static int dataBufferSize = 4096;

        public string IP;
        public ushort port;
        public int ID;
        public TCP tcp;
        public UDP udp;

        private bool isConnected = false;
        private delegate void PacketHandler(H3MP_Packet packet);
        private static PacketHandler[] packetHandlers;
        public static Dictionary<string, int> synchronizedScenes;
        public static H3MP_TrackedItemData[] items; // All tracked items, regardless of whos control they are under
        public static H3MP_TrackedSosigData[] sosigs; // All tracked Sosigs, regardless of whos control they are under
        public static H3MP_TrackedAutoMeaterData[] autoMeaters; // All tracked AutoMeaters, regardless of whos control they are under
        public static H3MP_TrackedEncryptionData[] encryptions; // All tracked TNH_EncryptionTarget, regardless of whos control they are under

        private void Awake()
        {
            singleton = this;
        }

        private void Start()
        {
            if (tcp == null)
            {
                tcp = new TCP();
                udp = new UDP();
            }
        }

        public void ConnectToServer()
        {
            if(tcp == null)
            {
                tcp = new TCP();
                udp = new UDP();
            }

            InitializeClientData();

            isConnected = true;
            tcp.Connect();
        }

        public class TCP
        {
            public TcpClient socket;

            public NetworkStream stream;
            private H3MP_Packet receivedData;
            public byte[] receiveBuffer;

            public void Connect()
            {
                socket = new TcpClient
                {
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize
                };

                receiveBuffer = new byte[dataBufferSize];
                Debug.Log("Making connection to " + singleton.IP + ":" + singleton.port);
                socket.BeginConnect(singleton.IP, singleton.port, ConnectCallback, socket);
                Debug.Log("connection begun");
            }

            private void ConnectCallback(IAsyncResult result)
            {
                Debug.Log("Connect callback");
                socket.EndConnect(result);

                if (!socket.Connected)
                {
                    return;
                }

                stream = socket.GetStream();

                receivedData = new H3MP_Packet();

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }

            public void SendData(H3MP_Packet packet)
            {
                try
                {
                    if(socket != null)
                    {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch(Exception ex)
                {
                    Debug.Log($"Error sending data to server via TCP: {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLength = stream.EndRead(result);
                    if (byteLength == 0)
                    {
                        singleton.Disconnect();
                        return;
                    }

                    byte[] data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);

                    receivedData.Reset(HandleData(data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Error receiving TCP data {ex}");
                    Disconnect();
                }
            }

            private bool HandleData(byte[] data)
            {
                int packetLength = 0;

                receivedData.SetBytes(data);

                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.ReadInt();
                    if(packetLength <= 0)
                    {
                        return true;
                    }
                }

                while(packetLength > 0 && packetLength <= receivedData.UnreadLength())
                {
                    byte[] packetBytes = receivedData.ReadBytes(packetLength);
                    H3MP_ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using(H3MP_Packet packet = new H3MP_Packet(packetBytes))
                        {
                            int packetID = packet.ReadInt();
                            packetHandlers[packetID](packet);
                        }
                    });

                    packetLength = 0;

                    if (receivedData.UnreadLength() >= 4)
                    {
                        packetLength = receivedData.ReadInt();
                        if (packetLength <= 0)
                        {
                            return true;
                        }
                    }
                }

                if(packetLength <= 1)
                {
                    return true;
                }

                return false;
            }

            private void Disconnect()
            {
                singleton.Disconnect();

                stream = null;
                receiveBuffer = null;
                receivedData = null;
                socket = null;
            }
        }

        public class UDP
        {
            public UdpClient socket;
            public IPEndPoint endPoint;

            public UDP()
            {
                endPoint = new IPEndPoint(IPAddress.Parse(singleton.IP), singleton.port);
            }

            public void Connect(int localPort)
            {
                socket = new UdpClient(localPort);

                socket.Connect(endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                using(H3MP_Packet packet = new H3MP_Packet())
                {
                    SendData(packet);
                }
            }

            public void SendData(H3MP_Packet packet)
            {
                try
                {
                    packet.InsertInt(singleton.ID);
                    if(socket != null)
                    {
                        socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
                    }
                }
                catch(Exception ex)
                {
                    Debug.Log($"Error sending UDP data {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    byte[] data = socket.EndReceive(result, ref endPoint);
                    socket.BeginReceive(ReceiveCallback, null);

                    if(data.Length < 4)
                    {
                        singleton.Disconnect();
                        return;
                    }

                    HandleData(data);
                }
                catch(Exception ex)
                {
                    Debug.Log($"Error receiving UDP data {ex}");
                    Disconnect();
                }
            }

            private void HandleData(byte[] data)
            {
                using(H3MP_Packet packet = new H3MP_Packet(data))
                {
                    int packetLength = packet.ReadInt();
                    data = packet.ReadBytes(packetLength);
                }

                H3MP_ThreadManager.ExecuteOnMainThread(() =>
                {
                    using(H3MP_Packet packet = new H3MP_Packet(data))
                    {
                        int packetID = packet.ReadInt();
                        packetHandlers[packetID](packet);
                    }
                });
            }

            private void Disconnect()
            {
                singleton.Disconnect();

                endPoint = null;
                socket = null;
            }
        }

        private void InitializeClientData()
        {
            packetHandlers = new PacketHandler[]
            {
                null,
                H3MP_ClientHandle.Welcome,
                H3MP_ClientHandle.SpawnPlayer,
                H3MP_ClientHandle.PlayerState,
                H3MP_ClientHandle.PlayerScene,
                H3MP_ClientHandle.AddSyncScene,
                H3MP_ClientHandle.TrackedItems,
                H3MP_ClientHandle.TrackedItem,
                null, // Unused ServerPackets.takeControl
                H3MP_ClientHandle.GiveControl,
                H3MP_ClientHandle.DestroyItem,
                H3MP_ClientHandle.ItemParent,
                H3MP_ClientHandle.ConnectSync,
                H3MP_ClientHandle.WeaponFire,
                H3MP_ClientHandle.PlayerDamage,
                H3MP_ClientHandle.TrackedSosig,
                H3MP_ClientHandle.TrackedSosigs,
                H3MP_ClientHandle.GiveSosigControl,
                H3MP_ClientHandle.DestroySosig,
                H3MP_ClientHandle.SosigPickUpItem,
                H3MP_ClientHandle.SosigPlaceItemIn,
                H3MP_ClientHandle.SosigDropSlot,
                H3MP_ClientHandle.SosigHandDrop,
                H3MP_ClientHandle.SosigConfigure,
                H3MP_ClientHandle.SosigLinkRegisterWearable,
                H3MP_ClientHandle.SosigLinkDeRegisterWearable,
                H3MP_ClientHandle.SosigSetIFF,
                H3MP_ClientHandle.SosigSetOriginalIFF,
                H3MP_ClientHandle.SosigLinkDamage,
                H3MP_ClientHandle.SosigDamageData,
                H3MP_ClientHandle.SosigWearableDamage,
                H3MP_ClientHandle.SosigLinkExplodes,
                H3MP_ClientHandle.SosigDies,
                H3MP_ClientHandle.SosigClear,
                H3MP_ClientHandle.SosigSetBodyState,
                H3MP_ClientHandle.PlaySosigFootStepSound,
                H3MP_ClientHandle.SosigSpeakState,
                H3MP_ClientHandle.SosigSetCurrentOrder,
                H3MP_ClientHandle.SosigVaporize,
                H3MP_ClientHandle.SosigRequestHitDecal,
                H3MP_ClientHandle.SosigLinkBreak,
                H3MP_ClientHandle.SosigLinkSever,
                H3MP_ClientHandle.RequestUpToDateObjects,
                H3MP_ClientHandle.PlayerInstance,
                H3MP_ClientHandle.AddTNHInstance,
                H3MP_ClientHandle.AddTNHCurrentlyPlaying,
                H3MP_ClientHandle.RemoveTNHCurrentlyPlaying,
                H3MP_ClientHandle.SetTNHProgression,
                H3MP_ClientHandle.SetTNHEquipment,
                H3MP_ClientHandle.SetTNHHealthMode,
                H3MP_ClientHandle.SetTNHTargetMode,
                H3MP_ClientHandle.SetTNHAIDifficulty,
                H3MP_ClientHandle.SetTNHRadarMode,
                H3MP_ClientHandle.SetTNHItemSpawnerMode,
                H3MP_ClientHandle.SetTNHBackpackMode,
                H3MP_ClientHandle.SetTNHHealthMult,
                H3MP_ClientHandle.SetTNHSosigGunReload,
                H3MP_ClientHandle.SetTNHSeed,
                H3MP_ClientHandle.SetTNHLevelIndex,
                H3MP_ClientHandle.AddInstance,
                H3MP_ClientHandle.SetTNHController,
                H3MP_ClientHandle.TNHData,
                H3MP_ClientHandle.TNHPlayerDied,
                H3MP_ClientHandle.TNHAddTokens,
                H3MP_ClientHandle.TNHSetLevel,
                H3MP_ClientHandle.TrackedAutoMeater,
                H3MP_ClientHandle.TrackedAutoMeaters,
                H3MP_ClientHandle.DestroyAutoMeater,
                H3MP_ClientHandle.GiveAutoMeaterControl,
                H3MP_ClientHandle.AutoMeaterSetState,
                H3MP_ClientHandle.AutoMeaterSetBladesActive,
                H3MP_ClientHandle.AutoMeaterDamage,
                H3MP_ClientHandle.AutoMeaterFirearmFireShot,
                H3MP_ClientHandle.AutoMeaterFirearmFireAtWill,
                H3MP_ClientHandle.AutoMeaterHitZoneDamage,
                H3MP_ClientHandle.AutoMeaterHitZoneDamageData,
                H3MP_ClientHandle.TNHSosigKill,
                H3MP_ClientHandle.TNHHoldPointSystemNode,
                H3MP_ClientHandle.TNHHoldBeginChallenge,
                H3MP_ClientHandle.TNHSetPhaseTake,
                H3MP_ClientHandle.TNHHoldCompletePhase,
                H3MP_ClientHandle.TNHHoldShutDown,
                H3MP_ClientHandle.TNHSetPhaseComplete,
                H3MP_ClientHandle.TNHSetPhase,
                H3MP_ClientHandle.TrackedEncryptions,
                H3MP_ClientHandle.TrackedEncryption,
                H3MP_ClientHandle.GiveEncryptionControl,
                H3MP_ClientHandle.DestroyEncryption,
                H3MP_ClientHandle.EncryptionDamage,
                H3MP_ClientHandle.EncryptionDamageData,
                H3MP_ClientHandle.EncryptionRespawnSubTarg,
                H3MP_ClientHandle.EncryptionSpawnGrowth,
                H3MP_ClientHandle.EncryptionRecursiveInit,
                H3MP_ClientHandle.EncryptionResetGrowth,
                H3MP_ClientHandle.EncryptionDisableSubtarg,
                H3MP_ClientHandle.EncryptionSubDamage,
                H3MP_ClientHandle.ShatterableCrateDamage,
                H3MP_ClientHandle.ShatterableCrateDestroy,
                H3MP_ClientHandle.InitTNHInstances,
                H3MP_ClientHandle.SosigWeaponFire,
                H3MP_ClientHandle.SosigWeaponShatter,
                H3MP_ClientHandle.SosigWeaponDamage,
                H3MP_ClientHandle.LAPD2019Fire,
                H3MP_ClientHandle.LAPD2019LoadBattery,
                H3MP_ClientHandle.LAPD2019ExtractBattery,
                H3MP_ClientHandle.MinigunFire,
                H3MP_ClientHandle.AttachableFirearmFire,
                H3MP_ClientHandle.BreakActionWeaponFire,
                H3MP_ClientHandle.PlayerIFF,
                H3MP_ClientHandle.UberShatterableShatter,
                H3MP_ClientHandle.TNHHoldPointBeginAnalyzing,
            };

            // All vanilla scenes can be synced by default
            synchronizedScenes = new Dictionary<string, int>();
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                synchronizedScenes.Add(System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i)), 0);
            }

            items = new H3MP_TrackedItemData[100];

            sosigs = new H3MP_TrackedSosigData[100];

            autoMeaters = new H3MP_TrackedAutoMeaterData[100];

            encryptions = new H3MP_TrackedEncryptionData[100];

            Debug.Log("Initialized client");
        }

        public static void AddTrackedItem(H3MP_TrackedItemData trackedItem, string scene, int instance)
        {
            // Adjust items size to acommodate if necessary
            if (items.Length <= trackedItem.trackedID)
            {
                IncreaseItemsSize(trackedItem.trackedID);
            }

            if (trackedItem.controller == H3MP_Client.singleton.ID)
            {
                // If we already control the item it is because we are the one who send the item to the server
                // We just need to update the tracked ID of the item
                H3MP_GameManager.items[trackedItem.localTrackedID].trackedID = trackedItem.trackedID;

                // Add the item to client global list
                items[trackedItem.trackedID] = H3MP_GameManager.items[trackedItem.localTrackedID];

                items[trackedItem.trackedID].OnTrackedIDReceived();
            }
            else
            {
                trackedItem.localTrackedID = -1;

                // Add the item to client global list
                items[trackedItem.trackedID] = trackedItem;

                // Instantiate item if it is in the current scene
                if (scene.Equals(SceneManager.GetActiveScene().name) && instance == H3MP_GameManager.instance)
                {
                    AnvilManager.Run(trackedItem.Instantiate());
                }
            }
        }

        public static void AddTrackedSosig(H3MP_TrackedSosigData trackedSosig, string scene, int instance)
        {
            Debug.Log("Received order to add a sosig");
            // Adjust sosigs size to acommodate if necessary
            if (sosigs.Length <= trackedSosig.trackedID)
            {
                IncreaseSosigsSize(trackedSosig.trackedID);
            }

            if (trackedSosig.controller == H3MP_Client.singleton.ID)
            {
                // If we already control the sosig it is because we are the one who sent the sosig to the server
                // We just need to update the tracked ID of the sosig
                H3MP_GameManager.sosigs[trackedSosig.localTrackedID].trackedID = trackedSosig.trackedID;

                // Add the sosig to client global list
                sosigs[trackedSosig.trackedID] = H3MP_GameManager.sosigs[trackedSosig.localTrackedID];

                // Send queued up orders
                sosigs[trackedSosig.trackedID].OnTrackedIDReceived();

                // Only send latest data if not destroyed
                if (sosigs[trackedSosig.trackedID] != null)
                {
                    sosigs[trackedSosig.trackedID].Update(true);

                    // Send the latest full data to server again in case anything happened while we were waiting for tracked ID
                    H3MP_ClientSend.TrackedSosig(sosigs[trackedSosig.trackedID], scene, instance);
                }
            }
            else
            {
                if(sosigs[trackedSosig.trackedID] == null)
                {
                    trackedSosig.localTrackedID = -1;

                    // Add the sosig to client global list
                    sosigs[trackedSosig.trackedID] = trackedSosig;

                    // Instantiate sosig if it is in the current scene
                    if (scene.Equals(SceneManager.GetActiveScene().name) && instance == H3MP_GameManager.instance)
                    {
                        AnvilManager.Run(trackedSosig.Instantiate());
                    }
                }
                else // This is an initial update sosig data
                {
                    H3MP_TrackedSosigData trackedSosigData = sosigs[trackedSosig.trackedID];

                    // Instantiate sosig if it is in the current scene if not instantiated already
                    // This could be the case if joining a scene with sosigs we already have the data for
                    if (trackedSosigData.physicalObject == null)
                    {
                        if (scene.Equals(SceneManager.GetActiveScene().name) && instance == H3MP_GameManager.instance)
                        {
                            AnvilManager.Run(trackedSosigData.Instantiate());
                        }
                    }

                    trackedSosigData.Update(trackedSosig, true);
                }
            }
        }

        public static void AddTrackedAutoMeater(H3MP_TrackedAutoMeaterData trackedAutoMeater, string scene, int instance)
        {
            // Adjust AutoMeaters size to acommodate if necessary
            if (autoMeaters.Length <= trackedAutoMeater.trackedID)
            {
                IncreaseAutoMeatersSize(trackedAutoMeater.trackedID);
            }

            if (trackedAutoMeater.controller == H3MP_Client.singleton.ID)
            {
                // If we already control the AutoMeater it is because we are the one who sent the AutoMeater to the server
                // We just need to update the tracked ID of the AutoMeater
                H3MP_GameManager.autoMeaters[trackedAutoMeater.localTrackedID].trackedID = trackedAutoMeater.trackedID;

                // Add the AutoMeater to client global list
                autoMeaters[trackedAutoMeater.trackedID] = H3MP_GameManager.autoMeaters[trackedAutoMeater.localTrackedID];

                // Send queued up orders
                autoMeaters[trackedAutoMeater.trackedID].OnTrackedIDReceived();
            }
            else
            {
                trackedAutoMeater.localTrackedID = -1;

                // Add the AutoMeater to client global list
                autoMeaters[trackedAutoMeater.trackedID] = trackedAutoMeater;

                // Instantiate AutoMeater if it is in the current scene
                if (scene.Equals(SceneManager.GetActiveScene().name) && instance == H3MP_GameManager.instance)
                {
                    AnvilManager.Run(trackedAutoMeater.Instantiate());
                }
            }
        }

        public static void AddTrackedEncryption(H3MP_TrackedEncryptionData trackedEncryption, string scene, int instance)
        {
            Debug.Log("Received order to add an Encryption");
            // Adjust Encryptions size to acommodate if necessary
            if (encryptions.Length <= trackedEncryption.trackedID)
            {
                IncreaseEncryptionsSize(trackedEncryption.trackedID);
            }

            if (trackedEncryption.controller == H3MP_Client.singleton.ID)
            {
                // If we already control the Encryption it is because we are the one who sent the Encryption to the server
                // We just need to update the tracked ID of the Encryption
                H3MP_GameManager.encryptions[trackedEncryption.localTrackedID].trackedID = trackedEncryption.trackedID;

                // Add the Encryption to client global list
                encryptions[trackedEncryption.trackedID] = H3MP_GameManager.encryptions[trackedEncryption.localTrackedID];

                // Send queued up orders
                encryptions[trackedEncryption.trackedID].OnTrackedIDReceived();

                // Only send latest data if not destroyed
                if (encryptions[trackedEncryption.trackedID] != null)
                {
                    encryptions[trackedEncryption.trackedID].Update(true);

                    // Send the latest full data to server again in case anything happened while we were waiting for tracked ID
                    H3MP_ClientSend.TrackedEncryption(encryptions[trackedEncryption.trackedID], scene, instance);
                }
            }
            else
            {
                if (encryptions[trackedEncryption.trackedID] == null)
                {
                    trackedEncryption.localTrackedID = -1;

                    // Add the Encryption to client global list
                    encryptions[trackedEncryption.trackedID] = trackedEncryption;

                    // Instantiate Encryption if it is in the current scene
                    if (scene.Equals(SceneManager.GetActiveScene().name) && instance == H3MP_GameManager.instance)
                    {
                        AnvilManager.Run(trackedEncryption.Instantiate());
                    }
                }
                else // This is an initial update sosig data
                {
                    H3MP_TrackedEncryptionData trackedEncryptionData = encryptions[trackedEncryption.trackedID];

                    // Instantiate Encryption if it is in the current scene if not instantiated already
                    // This could be the case if joining a scene with encryptions we already have the data for
                    if (trackedEncryptionData.physicalObject == null)
                    {
                        if (scene.Equals(SceneManager.GetActiveScene().name) && instance == H3MP_GameManager.instance)
                        {
                            AnvilManager.Run(trackedEncryptionData.Instantiate());
                        }
                    }

                    trackedEncryptionData.Update(trackedEncryption, true);
                }
            }
        }

        private static void IncreaseItemsSize(int minimum)
        {
            int minCapacity = items.Length;
            while(minCapacity <= minimum)
            {
                minCapacity += 100;
            }
            H3MP_TrackedItemData[] tempItems = items;
            items = new H3MP_TrackedItemData[minCapacity];
            for (int i = 0; i < tempItems.Length; ++i)
            {
                items[i] = tempItems[i];
            }
        }

        private static void IncreaseSosigsSize(int minimum)
        {
            int minCapacity = sosigs.Length;
            while(minCapacity <= minimum)
            {
                minCapacity += 100;
            }
            H3MP_TrackedSosigData[] tempSosigs = sosigs;
            sosigs = new H3MP_TrackedSosigData[minCapacity];
            for (int i = 0; i < tempSosigs.Length; ++i)
            {
                sosigs[i] = tempSosigs[i];
            }
        }

        private static void IncreaseAutoMeatersSize(int minimum)
        {
            int minCapacity = autoMeaters.Length;
            while(minCapacity <= minimum)
            {
                minCapacity += 100;
            }
            H3MP_TrackedAutoMeaterData[] tempAutoMeaters = autoMeaters;
            autoMeaters = new H3MP_TrackedAutoMeaterData[minCapacity];
            for (int i = 0; i < tempAutoMeaters.Length; ++i)
            {
                autoMeaters[i] = tempAutoMeaters[i];
            }
        }

        private static void IncreaseEncryptionsSize(int minimum)
        {
            int minCapacity = encryptions.Length;
            while(minCapacity <= minimum)
            {
                minCapacity += 100;
            }
            H3MP_TrackedEncryptionData[] tempEncryptions = encryptions;
            encryptions = new H3MP_TrackedEncryptionData[minCapacity];
            for (int i = 0; i < tempEncryptions.Length; ++i)
            {
                encryptions[i] = tempEncryptions[i];
            }
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        private void Disconnect()
        {
            if (isConnected)
            {
                isConnected = false;
                tcp.socket.Close();
                udp.socket.Close();

                Debug.Log("Disconnected from server.");
            }
        }
    }
}
