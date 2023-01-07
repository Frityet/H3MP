﻿using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static FistVR.TNH_Progression;
using static H3MP.H3MP_PlayerHitbox;
using static Valve.VR.SteamVR_ExternalCamera;
using static Valve.VR.SteamVR_TrackedObject;

namespace H3MP
{
    internal class H3MP_ServerHandle
    {
        public static void WelcomeReceived(int clientID, H3MP_Packet packet)
        {
            int clientIDCheck = packet.ReadInt();
            string username = packet.ReadString();
            string scene = packet.ReadString();
            int instance = packet.ReadInt();

            Debug.Log($"{H3MP_Server.clients[clientID].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {clientID}");

            if (clientID != clientIDCheck)
            {
                Debug.Log($"Player \"{username}\" (ID:{clientID}) has assumed wrong client ID ({clientIDCheck})");
            }

            // Spawn player to clients 
            H3MP_Server.clients[clientID].SendIntoGame(username, scene, instance);
        }

        public static void PlayerState(int clientID, H3MP_Packet packet)
        {
            H3MP_Player player = H3MP_Server.clients[clientID].player;

            player.position = packet.ReadVector3();
            player.rotation = packet.ReadQuaternion();
            player.headPos = packet.ReadVector3();
            player.headRot = packet.ReadQuaternion();
            player.torsoPos = packet.ReadVector3();
            player.torsoRot = packet.ReadQuaternion();
            player.leftHandPos = packet.ReadVector3();
            player.leftHandRot = packet.ReadQuaternion();
            player.rightHandPos = packet.ReadVector3();
            player.rightHandRot = packet.ReadQuaternion();
            player.health = packet.ReadFloat();
            player.maxHealth = packet.ReadInt();
            short additionalDataLength = packet.ReadShort();
            byte[] additionalData = null;
            if(additionalDataLength > 0)
            {
                additionalData = packet.ReadBytes(additionalDataLength);
            }

            H3MP_GameManager.UpdatePlayerState(player.ID, player.position, player.rotation, player.headPos, player.headRot, player.torsoPos, player.torsoRot,
                                               player.leftHandPos, player.leftHandRot,
                                               player.leftHandPos, player.leftHandRot,
                                               player.health, player.maxHealth, additionalData);
        }

        public static void PlayerScene(int clientID, H3MP_Packet packet)
        {
            H3MP_Player player = H3MP_Server.clients[clientID].player;

            string scene = packet.ReadString();

            H3MP_GameManager.UpdatePlayerScene(player.ID, scene);

            // Send to all other clients
            H3MP_ServerSend.PlayerScene(player.ID, scene);

            List<int> waitingFromClients = new List<int>();

            // Request most up to date items from relevant clients so we can send them to the client when it is ready to receive them
            if (H3MP_GameManager.synchronizedScenes.ContainsKey(player.scene))
            {
                foreach (KeyValuePair<int, H3MP_ServerClient> otherClient in H3MP_Server.clients)
                {
                    if (otherClient.Value.tcp != null && otherClient.Value.tcp.socket != null && // If a client is connected at that index
                        !H3MP_Server.loadingClientsWaitingFrom.ContainsKey(otherClient.Key) && // If the client is not currently loading
                        otherClient.Key != clientID && otherClient.Value.player.scene.Equals(scene) && otherClient.Value.player.instance == player.instance)
                    {
                        if (H3MP_Server.clientsWaitingUpDate.ContainsKey(otherClient.Key))
                        {
                            H3MP_Server.clientsWaitingUpDate[otherClient.Key].Add(clientID);
                        }
                        else
                        {
                            H3MP_Server.clientsWaitingUpDate.Add(otherClient.Key, new List<int> { clientID });
                        }
                        H3MP_ServerSend.RequestUpToDateObjects(otherClient.Key, false, clientID);
                        waitingFromClients.Add(otherClient.Key);
                    }
                }
            }

            H3MP_Server.loadingClientsWaitingFrom.Add(clientID, waitingFromClients);

            Debug.Log("Synced with player who just joined scene");
        }

        public static void PlayerInstance(int clientID, H3MP_Packet packet)
        {
            H3MP_Player player = H3MP_Server.clients[clientID].player;

            int instance = packet.ReadInt();

            H3MP_GameManager.UpdatePlayerInstance(player.ID, instance);

            // Send to all other clients
            H3MP_ServerSend.PlayerInstance(player.ID, instance);

            // Request most up to date items from relevant clients so we can send them to the client when it is ready to receive them
            if (H3MP_GameManager.synchronizedScenes.ContainsKey(player.scene))
            {
                foreach (KeyValuePair<int, H3MP_ServerClient> otherClient in H3MP_Server.clients)
                {
                    if (otherClient.Value.tcp != null && otherClient.Value.tcp.socket != null && // If a client is connected at that index
                        !H3MP_Server.loadingClientsWaitingFrom.ContainsKey(otherClient.Key) && // If the client is not currently loading
                        otherClient.Key != clientID && otherClient.Value.player.scene.Equals(player.scene) && otherClient.Value.player.instance == instance)
                    {
                        if (H3MP_Server.clientsWaitingUpDate.ContainsKey(otherClient.Key))
                        {
                            H3MP_Server.clientsWaitingUpDate[otherClient.Key].Add(clientID);
                        }
                        else
                        {
                            H3MP_Server.clientsWaitingUpDate.Add(otherClient.Key, new List<int> { clientID });
                        }
                        H3MP_ServerSend.RequestUpToDateObjects(otherClient.Key, false, clientID);
                    }
                }
            }
            Debug.Log("Synced with player who just joined instance");
        }

        public static void AddTNHInstance(int clientID, H3MP_Packet packet)
        {
            int hostID = packet.ReadInt();
            bool letPeopleJoin = packet.ReadBool();
            int progressionTypeSetting = packet.ReadInt();
            int healthModeSetting = packet.ReadInt();
            int equipmentModeSetting = packet.ReadInt();
            int targetModeSetting = packet.ReadInt();
            int AIDifficultyModifier = packet.ReadInt();
            int radarModeModifier = packet.ReadInt();
            int itemSpawnerMode = packet.ReadInt();
            int backpackMode = packet.ReadInt();
            int healthMult = packet.ReadInt();
            int sosiggunShakeReloading = packet.ReadInt();
            int TNHSeed = packet.ReadInt();
            int levelIndex = packet.ReadInt();

            // Send to all clients
            H3MP_ServerSend.AddTNHInstance(H3MP_GameManager.AddNewTNHInstance(hostID, letPeopleJoin,
                                                                              progressionTypeSetting, healthModeSetting, equipmentModeSetting,
                                                                              targetModeSetting, AIDifficultyModifier, radarModeModifier, itemSpawnerMode, backpackMode,
                                                                              healthMult, sosiggunShakeReloading, TNHSeed, levelIndex));
        }

        public static void AddInstance(int clientID, H3MP_Packet packet)
        {
            // Send to all clients
            H3MP_ServerSend.AddInstance(H3MP_GameManager.AddNewInstance());
        }

        public static void AddSyncScene(int clientID, H3MP_Packet packet)
        {
            string scene = packet.ReadString();

            H3MP_GameManager.synchronizedScenes.Add(scene, clientID);

            // Send to all other clients
            H3MP_ServerSend.AddSyncScene(clientID, scene);
        }

        public static void TrackedItems(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedItems from packet
            int count = packet.ReadShort();
            for(int i=0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedItem(packet.ReadTrackedItem());
            }
        }

        public static void TrackedSosigs(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedSosigs from packet
            int count = packet.ReadShort();
            for(int i=0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedSosig(packet.ReadTrackedSosig());
            }
        }

        public static void TrackedAutoMeaters(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedAutoMeaters from packet
            int count = packet.ReadShort();
            for(int i=0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedAutoMeater(packet.ReadTrackedAutoMeater());
            }
        }

        public static void TrackedEncryptions(int clientID, H3MP_Packet packet)
        {
            // Reconstruct passed trackedEncryptions from packet
            int count = packet.ReadShort();
            for(int i=0; i < count; ++i)
            {
                H3MP_GameManager.UpdateTrackedEncryption(packet.ReadTrackedEncryption());
            }
        }

        public static void TakeControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            H3MP_TrackedItemData trackedItem = H3MP_Server.items[trackedID];

            // Update locally
            if (trackedItem.controller == 0)
            {
                Mod.SetKinematicRecursive(trackedItem.physicalItem.transform, true);
                H3MP_GameManager.items[trackedItem.localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                H3MP_GameManager.items[trackedItem.localTrackedID].localTrackedID = trackedItem.localTrackedID;
                H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                trackedItem.localTrackedID = -1;
            }
            trackedItem.controller = clientID;

            // Send to all other clients
            H3MP_ServerSend.GiveControl(trackedID, clientID);
        }

        public static void GiveControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newController = packet.ReadInt();

            // Update locally
            H3MP_TrackedItemData trackedItem = H3MP_Server.items[trackedID];

            if (trackedItem.controller != 0 && newController == 0)
            {
                // Only want to active rigidbody if not parented to another tracked item
                if (trackedItem.parent == -1)
                {
                    Mod.SetKinematicRecursive(trackedItem.physicalItem.transform, false);
                }
                trackedItem.localTrackedID = H3MP_GameManager.items.Count;
                H3MP_GameManager.items.Add(trackedItem);
            }
            else if(trackedItem.controller == 0 && newController != 0)
            {
                Mod.SetKinematicRecursive(trackedItem.physicalItem.transform, true);
                H3MP_GameManager.items[trackedItem.localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                H3MP_GameManager.items[trackedItem.localTrackedID].localTrackedID = trackedItem.localTrackedID;
                H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                trackedItem.localTrackedID = -1;
            }
            trackedItem.controller = newController;

            // Send to all other clients
            H3MP_ServerSend.GiveControl(trackedID, newController);
        }

        public static void GiveSosigControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newController = packet.ReadInt();

            // Update locally
            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[trackedID];

            if (trackedSosig.controller != 0 && newController == 0)
            {
                trackedSosig.localTrackedID = H3MP_GameManager.sosigs.Count;
                if(trackedSosig.physicalObject != null)
                {
                    GM.CurrentAIManager.RegisterAIEntity(trackedSosig.physicalObject.physicalSosigScript.E);
                    trackedSosig.physicalObject.physicalSosigScript.CoreRB.isKinematic = false;
                }
                H3MP_GameManager.sosigs.Add(trackedSosig);
            }
            else if(trackedSosig.controller == 0 && newController != 0)
            {
                H3MP_GameManager.sosigs[trackedSosig.localTrackedID] = H3MP_GameManager.sosigs[H3MP_GameManager.sosigs.Count - 1];
                H3MP_GameManager.sosigs[trackedSosig.localTrackedID].localTrackedID = trackedSosig.localTrackedID;
                H3MP_GameManager.sosigs.RemoveAt(H3MP_GameManager.sosigs.Count - 1);
                trackedSosig.localTrackedID = -1;
                if (trackedSosig.physicalObject != null)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(trackedSosig.physicalObject.physicalSosigScript.E);
                    trackedSosig.physicalObject.physicalSosigScript.CoreRB.isKinematic = true;
                }
            }
            trackedSosig.controller = newController;

            // Send to all other clients
            H3MP_ServerSend.GiveSosigControl(trackedID, newController);
        }

        public static void GiveAutoMeaterControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newController = packet.ReadInt();

            // Update locally
            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[trackedID];

            if (trackedAutoMeater.controller != 0 && newController == 0)
            {
                trackedAutoMeater.localTrackedID = H3MP_GameManager.autoMeaters.Count;
                if(trackedAutoMeater.physicalObject != null)
                {
                    GM.CurrentAIManager.RegisterAIEntity(trackedAutoMeater.physicalObject.physicalAutoMeaterScript.E);
                    trackedAutoMeater.physicalObject.physicalAutoMeaterScript.RB.isKinematic = false;
                }
                H3MP_GameManager.autoMeaters.Add(trackedAutoMeater);
            }
            else if(trackedAutoMeater.controller == 0 && newController != 0)
            {
                H3MP_GameManager.autoMeaters[trackedAutoMeater.localTrackedID] = H3MP_GameManager.autoMeaters[H3MP_GameManager.autoMeaters.Count - 1];
                H3MP_GameManager.autoMeaters[trackedAutoMeater.localTrackedID].localTrackedID = trackedAutoMeater.localTrackedID;
                H3MP_GameManager.autoMeaters.RemoveAt(H3MP_GameManager.autoMeaters.Count - 1);
                trackedAutoMeater.localTrackedID = -1;
                if (trackedAutoMeater.physicalObject != null)
                {
                    GM.CurrentAIManager.DeRegisterAIEntity(trackedAutoMeater.physicalObject.physicalAutoMeaterScript.E);
                    trackedAutoMeater.physicalObject.physicalAutoMeaterScript.RB.isKinematic = true;
                }
            }
            trackedAutoMeater.controller = newController;

            // Send to all other clients
            H3MP_ServerSend.GiveAutoMeaterControl(trackedID, newController);
        }

        public static void GiveEncryptionControl(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newController = packet.ReadInt();

            // Update locally
            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Server.encryptions[trackedID];

            if (trackedEncryption.controller != 0 && newController == 0)
            {
                trackedEncryption.localTrackedID = H3MP_GameManager.encryptions.Count;
                H3MP_GameManager.encryptions.Add(trackedEncryption);
            }
            else if(trackedEncryption.controller == 0 && newController != 0)
            {
                H3MP_GameManager.encryptions[trackedEncryption.localTrackedID] = H3MP_GameManager.encryptions[H3MP_GameManager.encryptions.Count - 1];
                H3MP_GameManager.encryptions[trackedEncryption.localTrackedID].localTrackedID = trackedEncryption.localTrackedID;
                H3MP_GameManager.encryptions.RemoveAt(H3MP_GameManager.encryptions.Count - 1);
                trackedEncryption.localTrackedID = -1;
            }
            trackedEncryption.controller = newController;

            // Send to all other clients
            H3MP_ServerSend.GiveEncryptionControl(trackedID, newController);
        }

        public static void DestroySosig(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();
            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[trackedID];

            if (trackedSosig.physicalObject != null)
            {
                H3MP_GameManager.trackedSosigBySosig.Remove(trackedSosig.physicalObject.physicalSosigScript);
                trackedSosig.physicalObject.sendDestroy = false;
                GameObject.Destroy(trackedSosig.physicalObject.gameObject);
            }

            if (trackedSosig.localTrackedID != -1)
            {
                H3MP_GameManager.sosigs[trackedSosig.localTrackedID] = H3MP_GameManager.sosigs[H3MP_GameManager.sosigs.Count - 1];
                H3MP_GameManager.sosigs[trackedSosig.localTrackedID].localTrackedID = trackedSosig.localTrackedID;
                H3MP_GameManager.sosigs.RemoveAt(H3MP_GameManager.sosigs.Count - 1);
            }

            if (removeFromList)
            {
                H3MP_Server.sosigs[trackedID] = null;
                H3MP_Server.availableSosigIndices.Add(trackedID);
            }

            H3MP_ServerSend.DestroySosig(trackedID, removeFromList, clientID);
        }

        public static void DestroyAutoMeater(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();
            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[trackedID];

            if (trackedAutoMeater.physicalObject != null)
            {
                H3MP_GameManager.trackedAutoMeaterByAutoMeater.Remove(trackedAutoMeater.physicalObject.physicalAutoMeaterScript);
                trackedAutoMeater.physicalObject.sendDestroy = false;
                GameObject.Destroy(trackedAutoMeater.physicalObject.gameObject);
            }

            if (trackedAutoMeater.localTrackedID != -1)
            {
                H3MP_GameManager.autoMeaters[trackedAutoMeater.localTrackedID] = H3MP_GameManager.autoMeaters[H3MP_GameManager.autoMeaters.Count - 1];
                H3MP_GameManager.autoMeaters[trackedAutoMeater.localTrackedID].localTrackedID = trackedAutoMeater.localTrackedID;
                H3MP_GameManager.autoMeaters.RemoveAt(H3MP_GameManager.autoMeaters.Count - 1);
            }

            if (removeFromList)
            {
                H3MP_Server.autoMeaters[trackedID] = null;
                H3MP_Server.availableAutoMeaterIndices.Add(trackedID);
            }

            H3MP_ServerSend.DestroyAutoMeater(trackedID, removeFromList, clientID);
        }

        public static void DestroyEncryption(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();
            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Server.encryptions[trackedID];

            if (trackedEncryption.physicalObject != null)
            {
                H3MP_GameManager.trackedEncryptionByEncryption.Remove(trackedEncryption.physicalObject.physicalEncryptionScript);
                trackedEncryption.physicalObject.sendDestroy = false;
                GameObject.Destroy(trackedEncryption.physicalObject.gameObject);
            }

            if (trackedEncryption.localTrackedID != -1)
            {
                H3MP_GameManager.encryptions[trackedEncryption.localTrackedID] = H3MP_GameManager.encryptions[H3MP_GameManager.encryptions.Count - 1];
                H3MP_GameManager.encryptions[trackedEncryption.localTrackedID].localTrackedID = trackedEncryption.localTrackedID;
                H3MP_GameManager.encryptions.RemoveAt(H3MP_GameManager.encryptions.Count - 1);
            }

            if (removeFromList)
            {
                H3MP_Server.encryptions[trackedID] = null;
                H3MP_Server.availableEncryptionIndices.Add(trackedID);
            }

            H3MP_ServerSend.DestroyEncryption(trackedID, removeFromList, clientID);
        }

        public static void DestroyItem(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool removeFromList = packet.ReadBool();
            H3MP_TrackedItemData trackedItem = H3MP_Server.items[trackedID];
            Debug.Log("Received destroy order for " + trackedItem.itemID);

            if (trackedItem.physicalItem != null)
            {
                H3MP_GameManager.trackedItemByItem.Remove(trackedItem.physicalItem.physicalObject);
                if (trackedItem.physicalItem.physicalObject is SosigWeaponPlayerInterface)
                {
                    H3MP_GameManager.trackedItemBySosigWeapon.Remove((trackedItem.physicalItem.physicalObject as SosigWeaponPlayerInterface).W);
                }
                trackedItem.physicalItem.sendDestroy = false;
                GameObject.Destroy(trackedItem.physicalItem.gameObject);
            }

            if (trackedItem.localTrackedID != -1)
            {
                H3MP_GameManager.items[trackedItem.localTrackedID] = H3MP_GameManager.items[H3MP_GameManager.items.Count - 1];
                H3MP_GameManager.items[trackedItem.localTrackedID].localTrackedID = trackedItem.localTrackedID;
                H3MP_GameManager.items.RemoveAt(H3MP_GameManager.items.Count - 1);
                trackedItem.localTrackedID = -1;
            }

            if (removeFromList)
            {
                H3MP_Server.items[trackedID] = null;
                H3MP_Server.availableItemIndices.Add(trackedID);
            }

            H3MP_ServerSend.DestroyItem(trackedID, removeFromList, clientID);
        }

        public static void TrackedItem(int clientID, H3MP_Packet packet)
        {
            H3MP_Server.AddTrackedItem(packet.ReadTrackedItem(true), packet.ReadString(), packet.ReadInt(), clientID);
        }

        public static void TrackedSosig(int clientID, H3MP_Packet packet)
        {
            H3MP_Server.AddTrackedSosig(packet.ReadTrackedSosig(true), packet.ReadString(), packet.ReadInt(), clientID);
        }

        public static void TrackedAutoMeater(int clientID, H3MP_Packet packet)
        {
            H3MP_Server.AddTrackedAutoMeater(packet.ReadTrackedAutoMeater(true), packet.ReadString(), packet.ReadInt(), clientID);
        }

        public static void TrackedEncryption(int clientID, H3MP_Packet packet)
        {
            H3MP_Server.AddTrackedEncryption(packet.ReadTrackedEncryption(true), packet.ReadString(), packet.ReadInt(), clientID);
        }

        public static void ItemParent(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int newParentID = packet.ReadInt();

            H3MP_Server.items[trackedID].SetParent(newParentID);

            // Send to all other clients
            H3MP_ServerSend.ItemParent(trackedID, newParentID, clientID);
        }

        public static void WeaponFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FirePatch.positions = new List<Vector3>();
                FirePatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for(int i=0; i < count; ++i)
                {
                    FirePatch.positions.Add(packet.ReadVector3());
                    FirePatch.directions.Add(packet.ReadVector3());
                }
                FirePatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                H3MP_Server.items[trackedID].physicalItem.fireFunc();
            }

            // Send to other clients
            H3MP_ServerSend.WeaponFire(clientID, packet);
        }

        public static void SosigWeaponFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            float recoilMult = packet.ReadFloat();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireSosigWeaponPatch.positions = new List<Vector3>();
                FireSosigWeaponPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireSosigWeaponPatch.positions.Add(packet.ReadVector3());
                    FireSosigWeaponPatch.directions.Add(packet.ReadVector3());
                }
                FireSosigWeaponPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                H3MP_Server.items[trackedID].physicalItem.sosigWeaponfireFunc(recoilMult);
            }

            // Send to other clients
            H3MP_ServerSend.SosigWeaponFire(clientID, packet);
        }

        public static void MinigunFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireMinigunPatch.positions = new List<Vector3>();
                FireMinigunPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireMinigunPatch.positions.Add(packet.ReadVector3());
                    FireMinigunPatch.directions.Add(packet.ReadVector3());
                }
                FireSosigWeaponPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                Mod.Minigun_Fire.Invoke((Minigun)H3MP_Server.items[trackedID].physicalItem.physicalObject, null);
            }

            // Send to other clients
            H3MP_ServerSend.MinigunFire(clientID, packet);
        }

        public static void AttachableFirearmFire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool firedFromInterface = packet.ReadBool();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireAttachableFirearmPatch.positions = new List<Vector3>();
                FireAttachableFirearmPatch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireAttachableFirearmPatch.positions.Add(packet.ReadVector3());
                    FireAttachableFirearmPatch.directions.Add(packet.ReadVector3());
                }
                FireAttachableFirearmPatch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                H3MP_Server.items[trackedID].physicalItem.attachableFirearmFunc(firedFromInterface);
            }

            // Send to other clients
            H3MP_ServerSend.AttachableFirearmFire(clientID, packet);
        }

        public static void LAPD2019Fire(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                FireLAPD2019Patch.positions = new List<Vector3>();
                FireLAPD2019Patch.directions = new List<Vector3>();
                byte count = packet.ReadByte();
                for (int i = 0; i < count; ++i)
                {
                    FireLAPD2019Patch.positions.Add(packet.ReadVector3());
                    FireLAPD2019Patch.directions.Add(packet.ReadVector3());
                }
                FireLAPD2019Patch.overriden = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++Mod.skipNextFires;
                Mod.LAPD2019_Fire.Invoke((LAPD2019)H3MP_Server.items[trackedID].physicalItem.physicalObject, null);
            }

            // Send to other clients
            H3MP_ServerSend.LAPD2019Fire(clientID, packet);
        }

        public static void LAPD2019LoadBattery(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int batteryTrackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null && H3MP_Server.items[batteryTrackedID].physicalItem != null)
            {
                ++LAPD2019ActionPatch.loadBatterySkip;
                ((LAPD2019)H3MP_Server.items[trackedID].physicalItem.physicalObject).LoadBattery((LAPD2019Battery)H3MP_Server.items[batteryTrackedID].physicalItem.physicalObject);
                --LAPD2019ActionPatch.loadBatterySkip;
            }

            // Send to other clients
            H3MP_ServerSend.LAPD2019LoadBattery(clientID, trackedID, batteryTrackedID);
        }

        public static void LAPD2019ExtractBattery(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                ++LAPD2019ActionPatch.extractBatterySkip;
                ((LAPD2019)H3MP_Server.items[trackedID].physicalItem.physicalObject).ExtractBattery(null);
                --LAPD2019ActionPatch.extractBatterySkip;
            }

            // Send to other clients
            H3MP_ServerSend.LAPD2019ExtractBattery(clientID, trackedID);
        }

        public static void SosigWeaponShatter(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            // Update locally
            if (H3MP_Server.items[trackedID].physicalItem != null)
            {
                ++SosigWeaponShatterPatch.skip;
                typeof(SosigWeaponPlayerInterface).GetMethod("Shatter", BindingFlags.NonPublic | BindingFlags.Instance).Invoke((H3MP_Server.items[trackedID].physicalItem.physicalObject as SosigWeaponPlayerInterface).W, null);
                --SosigWeaponShatterPatch.skip;
            }

            // Send to other clients
            H3MP_ServerSend.SosigWeaponShatter(clientID, trackedID);
        }

        public static void AutoMeaterFirearmFireShot(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            Vector3 angles = packet.ReadVector3();

            // Update locally
            if (H3MP_Server.autoMeaters[trackedID].physicalObject != null)
            {
                // Set the muzzle angles to use
                AutoMeaterFirearmFireShotPatch.muzzleAngles = angles;
                AutoMeaterFirearmFireShotPatch.angleOverride = true;

                // Make sure we skip next fire so we don't have a firing feedback loop between clients
                ++AutoMeaterFirearmFireShotPatch.skip;
                Mod.AutoMeaterFirearm_FireShot.Invoke(H3MP_Server.autoMeaters[trackedID].physicalObject.physicalAutoMeaterScript.FireControl.Firearms[0], null);
                --AutoMeaterFirearmFireShotPatch.skip;
            }

            // Send to other clients
            H3MP_ServerSend.AutoMeaterFirearmFireShot(clientID, trackedID, angles);
        }

        public static void PlayerDamage(int clientID, H3MP_Packet packet)
        {
            int ID = packet.ReadInt();
            H3MP_PlayerHitbox.Part part = (H3MP_PlayerHitbox.Part)packet.ReadByte();
            Damage damage = packet.ReadDamage();

            if (ID == 0)
            {
                H3MP_GameManager.ProcessPlayerDamage(part, damage);
            }
            else
            {
                H3MP_ServerSend.PlayerDamage(ID, (byte)part, damage);
            }
        }

        public static void SosigPickUpItem(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                if (primaryHand)
                {
                    trackedSosig.physicalObject.physicalSosigScript.Hand_Primary.PickUp(H3MP_Server.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
                }
                else
                {
                    trackedSosig.physicalObject.physicalSosigScript.Hand_Secondary.PickUp(H3MP_Server.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
                }
            }

            H3MP_ServerSend.SosigPickUpItem(sosigTrackedID, itemTrackedID, primaryHand, clientID);
        }

        public static void SosigPlaceItemIn(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int itemTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                trackedSosig.physicalObject.physicalSosigScript.Inventory.Slots[slotIndex].PlaceObjectIn(H3MP_Server.items[itemTrackedID].physicalItem.GetComponent<SosigWeapon>());
            }

            H3MP_ServerSend.SosigPlaceItemIn(sosigTrackedID, slotIndex, itemTrackedID, clientID);
        }

        public static void SosigDropSlot(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            int slotIndex = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                trackedSosig.physicalObject.physicalSosigScript.Inventory.Slots[slotIndex].DetachHeldObject();
            }

            H3MP_ServerSend.SosigDropSlot(sosigTrackedID, slotIndex, clientID);
        }

        public static void SosigHandDrop(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            bool primaryHand = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                if (primaryHand)
                {
                    trackedSosig.physicalObject.physicalSosigScript.Hand_Primary.DropHeldObject();
                }
                else
                {
                    trackedSosig.physicalObject.physicalSosigScript.Hand_Secondary.DropHeldObject();
                }
            }

            H3MP_ServerSend.SosigHandDrop(sosigTrackedID, primaryHand, clientID);
        }

        public static void SosigConfigure(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Debug.Log("server handle sosig configure got called from client: " + clientID + " for sosig tracked ID: " + sosigTrackedID);
            SosigConfigTemplate config = packet.ReadSosigConfig();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                Debug.Log("\tFound trackedSosig, and it has physical, configuring ");
                trackedSosig.configTemplate = config;
                SosigConfigurePatch.skipConfigure = true;
                trackedSosig.physicalObject.physicalSosigScript.Configure(config);
            }

            H3MP_ServerSend.SosigConfigure(sosigTrackedID, config, clientID);
        }

        public static void SosigLinkRegisterWearable(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if (trackedSosig.wearables == null)
                {
                    trackedSosig.wearables = new List<List<string>>();
                    if (trackedSosig.physicalObject != null)
                    {
                        foreach (SosigLink link in trackedSosig.physicalObject.physicalSosigScript.Links)
                        {
                            trackedSosig.wearables.Add(new List<string>());
                        }
                    }
                    else
                    {
                        while (trackedSosig.wearables.Count <= linkIndex)
                        {
                            trackedSosig.wearables.Add(new List<string>());
                        }
                    }
                }
                trackedSosig.wearables[linkIndex].Add(wearableID);

                if (trackedSosig.physicalObject != null)
                {
                    AnvilManager.Run(trackedSosig.EquipWearable(linkIndex, wearableID, true));
                }
            }

            H3MP_ServerSend.SosigLinkRegisterWearable(sosigTrackedID, linkIndex, wearableID, clientID);
        }

        public static void SosigLinkDeRegisterWearable(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            string wearableID = packet.ReadString();

            if (sosigTrackedID != -1)
            {
                H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
                if (trackedSosig != null)
                {
                    if (trackedSosig.wearables != null)
                    {
                        if (trackedSosig.physicalObject != null)
                        {
                            FieldInfo wearablesField = typeof(SosigLink).GetField("m_wearables", BindingFlags.NonPublic | BindingFlags.Instance);
                            for (int i = 0; i < trackedSosig.wearables[linkIndex].Count; ++i)
                            {
                                if (trackedSosig.wearables[linkIndex][i].Equals(wearableID))
                                {
                                    trackedSosig.wearables[linkIndex].RemoveAt(i);
                                    if (trackedSosig.physicalObject != null)
                                    {
                                        ++SosigLinkActionPatch.skipDeRegisterWearable;
                                        trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].DeRegisterWearable((wearablesField.GetValue(trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex]) as List<SosigWearable>)[i]);
                                        --SosigLinkActionPatch.skipDeRegisterWearable;
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            trackedSosig.wearables[linkIndex].Remove(wearableID);
                        }
                    }
                }
            }

            H3MP_ServerSend.SosigLinkDeRegisterWearable(sosigTrackedID, linkIndex, wearableID, clientID);
        }

        public static void SosigSetIFF(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.IFF = IFF;
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigIFFPatch.skip;
                    trackedSosig.physicalObject.physicalSosigScript.SetIFF(IFF);
                    --SosigIFFPatch.skip;
                }
            }

            H3MP_ServerSend.SosigSetIFF(sosigTrackedID, IFF, clientID);
        }

        public static void SosigSetOriginalIFF(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte IFF = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                trackedSosig.IFF = IFF;
                if (trackedSosig.physicalObject != null)
                {
                    ++SosigIFFPatch.skip;
                    trackedSosig.physicalObject.physicalSosigScript.SetOriginalIFFTeam(IFF);
                    --SosigIFFPatch.skip;
                }
            }

            H3MP_ServerSend.SosigSetOriginalIFF(sosigTrackedID, IFF, clientID);
        }

        public static void SosigLinkDamage(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.controller == 0)
                {
                    if (trackedSosig.physicalObject != null)
                    {
                        ++SosigLinkDamagePatch.skip;
                        trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].Damage(damage);
                        --SosigLinkDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.SosigLinkDamage(trackedSosig, linkIndex, damage);
                }
            }
        }

        public static void AutoMeaterDamage(int clientID, H3MP_Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[autoMeaterTrackedID];
            if (trackedAutoMeater != null)
            {
                if(trackedAutoMeater.controller == 0)
                {
                    if (trackedAutoMeater.physicalObject != null)
                    {
                        ++AutoMeaterDamagePatch.skip;
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Damage(damage);
                        --AutoMeaterDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.AutoMeaterDamage(trackedAutoMeater, damage);
                }
            }
        }

        public static void AutoMeaterHitZoneDamage(int clientID, H3MP_Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();
            byte type = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[autoMeaterTrackedID];
            if (trackedAutoMeater != null)
            {
                if(trackedAutoMeater.controller == 0)
                {
                    if (trackedAutoMeater.physicalObject != null)
                    {
                        ++AutoMeaterHitZoneDamagePatch.skip;
                        trackedAutoMeater.hitZones[(AutoMeater.AMHitZoneType)type].Damage(damage);
                        --AutoMeaterHitZoneDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.AutoMeaterHitZoneDamage(trackedAutoMeater, type, damage);
                }
            }
        }

        public static void EncryptionDamage(int clientID, H3MP_Packet packet)
        {
            int encryptionTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Server.encryptions[encryptionTrackedID];
            if (trackedEncryption != null)
            {
                if(trackedEncryption.controller == 0)
                {
                    if (trackedEncryption.physicalObject != null)
                    {
                        ++EncryptionDamagePatch.skip;
                        trackedEncryption.physicalObject.physicalEncryptionScript.Damage(damage);
                        --EncryptionDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.EncryptionDamage(trackedEncryption, damage);
                }
            }
        }

        public static void AutoMeaterDamageData(int clientID, H3MP_Packet packet)
        {
            // TODO: if ever there is data we need to pass back from a auto meater damage call
        }

        public static void SosigWearableDamage(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte wearableIndex = packet.ReadByte();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.controller == 0)
                {
                    if (trackedSosig.physicalObject != null)
                    {
                        ++SosigWearableDamagePatch.skip;
                        (Mod.SosigLink_m_wearables.GetValue(trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex]) as List<SosigWearable>)[wearableIndex].Damage(damage);
                        --SosigWearableDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.SosigWearableDamage(trackedSosig, linkIndex, wearableIndex, damage);
                }
            }
        }

        public static void SosigDamageData(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.controller != 0 && trackedSosig.physicalObject != null)
                {
                    Sosig physicalSosig = trackedSosig.physicalObject.physicalSosigScript;
                    Mod.Sosig_m_isStunned.SetValue(physicalSosig, packet.ReadBool());
                    physicalSosig.m_stunTimeLeft = packet.ReadFloat();
                    physicalSosig.BodyState = (Sosig.SosigBodyState)packet.ReadByte();
                    Mod.Sosig_m_isOnOffMeshLinkField.SetValue(physicalSosig, packet.ReadBool());
                    physicalSosig.Agent.autoTraverseOffMeshLink = packet.ReadBool();
                    physicalSosig.Agent.enabled = packet.ReadBool();
                    List<CharacterJoint> joints = (List<CharacterJoint>)Mod.Sosig_m_joints.GetValue(physicalSosig);
                    byte jointCount = packet.ReadByte();
                    for (int i = 0; i < jointCount; ++i)
                    {
                        if (joints[i] != null)
                        {
                            SoftJointLimit softJointLimit = joints[i].lowTwistLimit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].lowTwistLimit = softJointLimit;
                            softJointLimit = joints[i].highTwistLimit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].highTwistLimit = softJointLimit;
                            softJointLimit = joints[i].swing1Limit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].swing1Limit = softJointLimit;
                            softJointLimit = joints[i].swing2Limit;
                            softJointLimit.limit = packet.ReadFloat();
                            joints[i].swing2Limit = softJointLimit;
                        }
                    }
                    Mod.Sosig_m_isCountingDownToStagger.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_staggerAmountToApply.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_recoveringFromBallisticState.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_recoveryFromBallisticLerp.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_tickDownToWrithe.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_recoveryFromBallisticTick.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_lastIFFDamageSource.SetValue(physicalSosig, packet.ReadByte());
                    Mod.Sosig_m_diedFromClass.SetValue(physicalSosig, (Damage.DamageClass)packet.ReadByte());
                    Mod.Sosig_m_isBlinded.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_blindTime.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_isFrozen.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_debuffTime_Freeze.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_receivedHeadShot.SetValue(physicalSosig, packet.ReadBool());
                    Mod.Sosig_m_timeSinceLastDamage.SetValue(physicalSosig, packet.ReadFloat());
                    Mod.Sosig_m_isConfused.SetValue(physicalSosig, packet.ReadBool());
                    physicalSosig.m_confusedTime = packet.ReadFloat();
                    Mod.Sosig_m_storedShudder.SetValue(physicalSosig, packet.ReadFloat());
                }
            }

            packet.readPos = 0;
            H3MP_ServerSend.SosigLinkDamageData(packet);
        }

        public static void EncryptionDamageData(int clientID, H3MP_Packet packet)
        {
            int encryptionTrackedID = packet.ReadInt();

            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Server.encryptions[encryptionTrackedID];
            if (trackedEncryption != null)
            {
                if(trackedEncryption.controller != 0 && trackedEncryption.physicalObject != null)
                {
                    Mod.TNH_EncryptionTarget_m_numHitsLeft.SetValue(trackedEncryption.physicalObject.physicalEncryptionScript, packet.ReadInt());
                }
            }

            packet.readPos = 0;
            H3MP_ServerSend.EncryptionDamageData(packet);
        }

        public static void AutoMeaterHitZoneDamageData(int clientID, H3MP_Packet packet)
        {
            int autoMeaterTrackedID = packet.ReadInt();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[autoMeaterTrackedID];
            if (trackedAutoMeater != null)
            {
                if(trackedAutoMeater.controller != 0 && trackedAutoMeater.physicalObject != null)
                {
                    AutoMeaterHitZone hitZone = trackedAutoMeater.hitZones[(AutoMeater.AMHitZoneType)packet.ReadByte()];
                    hitZone.ArmorThreshold = packet.ReadFloat();
                    hitZone.LifeUntilFailure = packet.ReadFloat();
                    if (packet.ReadBool()) // Destroyed
                    {
                        hitZone.BlowUp();
                    }
                }
            }

            packet.readPos = 0;
            H3MP_ServerSend.AutoMeaterHitZoneDamageData(packet);
        }

        public static void SosigLinkExplodes(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalObject != null)
                {
                    byte linkIndex = packet.ReadByte();
                    ++SosigLinkActionPatch.skipLinkExplodes;
                    trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].LinkExplodes((Damage.DamageClass)packet.ReadByte());
                    --SosigLinkActionPatch.skipLinkExplodes;
                }
            }

            H3MP_ServerSend.SosigLinkExplodes(packet, clientID);
        }

        public static void SosigDies(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalObject != null)
                {
                    byte damClass = packet.ReadByte();
                    byte deathType = packet.ReadByte();
                    ++SosigActionPatch.sosigDiesSkip;
                    trackedSosig.physicalObject.physicalSosigScript.SosigDies((Damage.DamageClass)damClass, (Sosig.SosigDeathType)deathType);
                    --SosigActionPatch.sosigDiesSkip;
                }
            }

            H3MP_ServerSend.SosigDies(packet, clientID);
        }

        public static void SosigClear(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalObject != null)
                {
                    ++SosigActionPatch.sosigClearSkip;
                    trackedSosig.physicalObject.physicalSosigScript.ClearSosig();
                    --SosigActionPatch.sosigClearSkip;
                }
            }

            H3MP_ServerSend.SosigClear(sosigTrackedID, clientID);
        }

        public static void SosigSetBodyState(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigBodyState bodyState = (Sosig.SosigBodyState)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null)
            {
                if(trackedSosig.physicalObject != null)
                {
                    ++SosigActionPatch.sosigSetBodyStateSkip;
                    Mod.Sosig_SetBodyState.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { bodyState });
                    --SosigActionPatch.sosigSetBodyStateSkip;
                }
            }

            H3MP_ServerSend.SosigSetBodyState(sosigTrackedID, bodyState, clientID);
        }

        public static void PlaySosigFootStepSound(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            FVRPooledAudioType audioType = (FVRPooledAudioType)packet.ReadByte();
            Vector3 position = packet.ReadVector3();
            Vector2 vol = packet.ReadVector2();
            Vector2 pitch = packet.ReadVector2();
            float delay = packet.ReadFloat();

            if (H3MP_Server.sosigs[sosigTrackedID].physicalObject != null)
            {
                // Ensure we have reference to sosig footsteps audio event
                if (Mod.sosigFootstepAudioEvent == null)
                {
                    Mod.sosigFootstepAudioEvent = H3MP_Server.sosigs[sosigTrackedID].physicalObject.physicalSosigScript.AudEvent_FootSteps;
                }

                // Play sound
                SM.PlayCoreSoundDelayedOverrides(audioType, Mod.sosigFootstepAudioEvent, position, vol, pitch, delay);
            }

            H3MP_ServerSend.PlaySosigFootStepSound(packet, clientID);
        }

        public static void SosigSpeakState(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                switch (currentOrder)
                {
                    case Sosig.SosigOrder.GuardPoint:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnWander });
                        break;
                    case Sosig.SosigOrder.Investigate:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnInvestigate });
                        break;
                    case Sosig.SosigOrder.SearchForEquipment:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnSearchingForGuns });
                        break;
                    case Sosig.SosigOrder.TakeCover:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnTakingCover });
                        break;
                    case Sosig.SosigOrder.Wander:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnWander });
                        break;
                    case Sosig.SosigOrder.Assault:
                        Mod.Sosig_Speak_State.Invoke(trackedSosig.physicalObject.physicalSosigScript, new object[] { trackedSosig.physicalObject.physicalSosigScript.Speech.OnAssault });
                        break;
                }
            }

            H3MP_ServerSend.SosigSpeakState(sosigTrackedID, currentOrder, clientID);
        }

        public static void SosigSetCurrentOrder(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            Sosig.SosigOrder currentOrder = (Sosig.SosigOrder)packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigActionPatch.sosigSetCurrentOrderSkip;
                trackedSosig.physicalObject.physicalSosigScript.SetCurrentOrder(currentOrder);
                --SosigActionPatch.sosigSetCurrentOrderSkip;
            }

            H3MP_ServerSend.SosigSetCurrentOrder(sosigTrackedID, currentOrder, clientID);
        }

        public static void SosigVaporize(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte iff = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigActionPatch.sosigVaporizeSkip;
                trackedSosig.physicalObject.physicalSosigScript.Vaporize(trackedSosig.physicalObject.physicalSosigScript.DamageFX_Vaporize, iff);
                --SosigActionPatch.sosigVaporizeSkip;
            }

            H3MP_ServerSend.SosigVaporize(sosigTrackedID, iff, clientID);
        }

        public static void SosigRequestHitDecal(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                Vector3 point = packet.ReadVector3();
                Vector3 normal = packet.ReadVector3();
                Vector3 edgeNormal = packet.ReadVector3();
                float scale = packet.ReadFloat();
                byte linkIndex = packet.ReadByte();
                ++SosigActionPatch.sosigRequestHitDecalSkip;
                trackedSosig.physicalObject.physicalSosigScript.RequestHitDecal(point, normal, edgeNormal, scale, trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex]);
                --SosigActionPatch.sosigRequestHitDecalSkip;
            }

            H3MP_ServerSend.SosigRequestHitDecal(packet, clientID);
        }

        public static void SosigLinkBreak(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            bool isStart = packet.ReadBool();
            byte damClass = packet.ReadByte();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigLinkActionPatch.sosigLinkBreakSkip;
                trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex].BreakJoint(isStart, (Damage.DamageClass)damClass);
                --SosigLinkActionPatch.sosigLinkBreakSkip;
            }

            H3MP_ServerSend.SosigLinkBreak(sosigTrackedID, linkIndex, isStart, damClass, clientID);
        }

        public static void SosigLinkSever(int clientID, H3MP_Packet packet)
        {
            int sosigTrackedID = packet.ReadInt();
            byte linkIndex = packet.ReadByte();
            byte damClass = packet.ReadByte();
            bool isPullApart = packet.ReadBool();

            H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[sosigTrackedID];
            if (trackedSosig != null && trackedSosig.physicalObject != null)
            {
                ++SosigLinkActionPatch.sosigLinkSeverSkip;
                Mod.SosigLink_SeverJoint.Invoke(trackedSosig.physicalObject.physicalSosigScript.Links[linkIndex], new object[] { damClass, isPullApart });
                --SosigLinkActionPatch.sosigLinkSeverSkip;
            }

            H3MP_ServerSend.SosigLinkSever(sosigTrackedID, linkIndex, damClass, isPullApart, clientID);
        }

        public static void UpToDateItems(int clientID, H3MP_Packet packet)
        {
            Debug.Log("Server received up to date items packet");
            // Reconstruct passed trackedItems from packet
            int count = packet.ReadShort();
            bool instantiate = packet.ReadBool();
            for (int i = 0; i < count; ++i)
            {
                H3MP_TrackedItemData trackedItem = packet.ReadTrackedItem(true);
                Debug.Log("\tItem: " +trackedItem.trackedID+", updating");
                H3MP_GameManager.UpdateTrackedItem(trackedItem, true);
                if (instantiate)
                {
                    Debug.Log("\tInstantiating");
                    AnvilManager.Run(H3MP_Server.items[trackedItem.trackedID].Instantiate());
                }
            }
        }

        public static void UpToDateSosigs(int clientID, H3MP_Packet packet)
        {
            Debug.Log("Server received up to date sosigs packet");
            // Reconstruct passed trackedSosigs from packet
            int count = packet.ReadShort();
            bool instantiate = packet.ReadBool();
            for (int i = 0; i < count; ++i)
            {
                H3MP_TrackedSosigData trackedSosig = packet.ReadTrackedSosig(true);
                Debug.Log("\tSosig: " + trackedSosig.trackedID + ", updating");
                H3MP_GameManager.UpdateTrackedSosig(trackedSosig, true);
                if (instantiate)
                {
                    Debug.Log("\tInstantiating");
                    AnvilManager.Run(H3MP_Server.sosigs[trackedSosig.trackedID].Instantiate());
                }
            }
        }

        public static void UpToDateAutoMeaters(int clientID, H3MP_Packet packet)
        {
            Debug.Log("Server received up to date AutoMeaters packet");
            // Reconstruct passed trackedAutoMeaters from packet
            int count = packet.ReadShort();
            bool instantiate = packet.ReadBool();
            for (int i = 0; i < count; ++i)
            {
                H3MP_TrackedAutoMeaterData trackedAutoMeater = packet.ReadTrackedAutoMeater(true);
                Debug.Log("\tAutoMeater: " + trackedAutoMeater.trackedID + ", updating");
                H3MP_GameManager.UpdateTrackedAutoMeater(trackedAutoMeater, true);
                if (instantiate)
                {
                    Debug.Log("\tInstantiating");
                    AnvilManager.Run(H3MP_Server.autoMeaters[trackedAutoMeater.trackedID].Instantiate());
                }
            }
        }

        public static void UpToDateEncryptions(int clientID, H3MP_Packet packet)
        {
            Debug.Log("Server received up to date Encryptions packet");
            // Reconstruct passed trackedEncryptions from packet
            int count = packet.ReadShort();
            bool instantiate = packet.ReadBool();
            for (int i = 0; i < count; ++i)
            {
                H3MP_TrackedEncryptionData trackedEncryption = packet.ReadTrackedEncryption(true);
                Debug.Log("\tEncryption: " + trackedEncryption.trackedID + ", updating");
                H3MP_GameManager.UpdateTrackedEncryption(trackedEncryption, true);
                if (instantiate)
                {
                    Debug.Log("\tInstantiating");
                    AnvilManager.Run(H3MP_Server.encryptions[trackedEncryption.trackedID].Instantiate());
                }
            }
        }

        public static void DoneLoadingScene(int clientID, H3MP_Packet packet)
        {
            if(H3MP_Server.loadingClientsWaitingFrom.TryGetValue(clientID, out List<int> otherClients))
            {
                bool stillWaiting = false;
                foreach(int otherCLientID in otherClients)
                {
                    if(H3MP_Server.clientsWaitingUpDate.TryGetValue(otherCLientID, out List<int> clientIDs))
                    {
                        if (clientIDs.Contains(clientID))
                        {
                            stillWaiting = true;
                            break;
                        }
                    }
                }

                if (!stillWaiting)
                {
                    H3MP_Server.clients[clientID].SendRelevantTrackedObjects();
                }

                H3MP_Server.loadingClientsWaitingFrom.Remove(clientID);
            }
        }

        public static void DoneSendingUpToDateObjects(int clientID, H3MP_Packet packet)
        {
            int forClient = packet.ReadInt();

            // If clients were waiting for this client to finish sending up to date objects
            if(H3MP_Server.clientsWaitingUpDate.TryGetValue(clientID, out List<int> waitingClients))
            {
                // If the relevant client is no longer loading or wasn't to begin with
                if(!H3MP_Server.loadingClientsWaitingFrom.ContainsKey(forClient))
                {
                    H3MP_Server.clients[clientID].SendRelevantTrackedObjects();
                }

                waitingClients.Remove(forClient);
                if(waitingClients.Count == 0)
                {
                    H3MP_Server.clientsWaitingUpDate.Remove(clientID);
                }
            }
        }

        public static void AddTNHCurrentlyPlaying(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances == null || !H3MP_GameManager.TNHInstances.ContainsKey(instance))
            {
                Debug.LogError("H3MP_ServerHandle: Received AddTNHCurrentlyPlaying packet with missing instance");
            }
            else
            {
                H3MP_GameManager.TNHInstances[instance].AddCurrentlyPlaying(false, clientID);

                H3MP_ServerSend.AddTNHCurrentlyPlaying(instance, clientID);
            }
        }

        public static void RemoveTNHCurrentlyPlaying(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance currentInstance))
            {
                currentInstance.RemoveCurrentlyPlaying(false, clientID);
                if (currentInstance.currentlyPlaying.Count == 0)
                {
                    currentInstance.Reset();
                }

                H3MP_ServerSend.RemoveTNHCurrentlyPlaying(instance, clientID);
            }
        }

        public static void SetTNHProgression(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].progressionTypeSetting = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.progressionSkip;
                Mod.currentTNHUIManager.OBS_Progression.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_Progression(i);
                GM.TNHOptions.ProgressionTypeSetting = (TNHSetting_ProgressionType)i;
                --TNH_UIManagerPatch.progressionSkip;
            }

            H3MP_ServerSend.SetTNHProgression(i, instance, clientID);
        }

        public static void SetTNHEquipment(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].equipmentModeSetting = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.equipmentSkip;
                Mod.currentTNHUIManager.OBS_Progression.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_EquipmentMode(i);
                GM.TNHOptions.EquipmentModeSetting = (TNHSetting_EquipmentMode)i;
                --TNH_UIManagerPatch.equipmentSkip;
            }

            H3MP_ServerSend.SetTNHEquipment(i, instance, clientID);
        }

        public static void SetTNHHealthMode(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].healthModeSetting = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.healthModeSkip;
                Mod.currentTNHUIManager.OBS_HealthMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_HealthMode(i);
                GM.TNHOptions.HealthModeSetting = (TNHSetting_HealthMode)i;
                --TNH_UIManagerPatch.healthModeSkip;
            }

            H3MP_ServerSend.SetTNHHealthMode(i, instance, clientID);
        }

        public static void SetTNHTargetMode(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].targetModeSetting = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.targetSkip;
                Mod.currentTNHUIManager.OBS_TargetMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_TargetMode(i);
                GM.TNHOptions.TargetModeSetting = (TNHSetting_TargetMode)i;
                --TNH_UIManagerPatch.targetSkip;
            }

            H3MP_ServerSend.SetTNHTargetMode(i, instance, clientID);
        }

        public static void SetTNHAIDifficulty(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].AIDifficultyModifier = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.AIDifficultySkip;
                Mod.currentTNHUIManager.OBS_AIDifficulty.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_AIDifficulty(i);
                GM.TNHOptions.AIDifficultyModifier = (TNHModifier_AIDifficulty)i;
                --TNH_UIManagerPatch.AIDifficultySkip;
            }

            H3MP_ServerSend.SetTNHAIDifficulty(i, instance, clientID);
        }

        public static void SetTNHRadarMode(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].radarModeModifier = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.radarSkip;
                Mod.currentTNHUIManager.OBS_AIRadarMode.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_AIRadarMode(i);
                GM.TNHOptions.RadarModeModifier = (TNHModifier_RadarMode)i;
                --TNH_UIManagerPatch.radarSkip;
            }

            H3MP_ServerSend.SetTNHRadarMode(i, instance, clientID);
        }

        public static void SetTNHItemSpawnerMode(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].itemSpawnerMode = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.itemSpawnerSkip;
                Mod.currentTNHUIManager.OBS_ItemSpawner.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_ItemSpawner(i);
                GM.TNHOptions.ItemSpawnerMode = (TNH_ItemSpawnerMode)i;
                --TNH_UIManagerPatch.itemSpawnerSkip;
            }

            H3MP_ServerSend.SetTNHItemSpawnerMode(i, instance, clientID);
        }

        public static void SetTNHBackpackMode(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].backpackMode = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.backpackSkip;
                Mod.currentTNHUIManager.OBS_Backpack.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_Backpack(i);
                GM.TNHOptions.BackpackMode = (TNH_BackpackMode)i;
                --TNH_UIManagerPatch.backpackSkip;
            }

            H3MP_ServerSend.SetTNHBackpackMode(i, instance, clientID);
        }

        public static void SetTNHHealthMult(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].healthMult = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.healthMultSkip;
                Mod.currentTNHUIManager.OBS_HealthMult.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_HealthMult(i);
                GM.TNHOptions.HealthMult = (TNH_HealthMult)i;
                --TNH_UIManagerPatch.healthMultSkip;
            }

            H3MP_ServerSend.SetTNHHealthMult(i, instance, clientID);
        }

        public static void SetTNHSosigGunReload(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].sosiggunShakeReloading = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.sosigGunReloadSkip;
                Mod.currentTNHUIManager.OBS_SosiggunReloading.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_SosiggunShakeReloading(i);
                GM.TNHOptions.SosiggunShakeReloading = (TNH_SosiggunShakeReloading)i;
                --TNH_UIManagerPatch.sosigGunReloadSkip;
            }

            H3MP_ServerSend.SetTNHSosigGunReload(i, instance, clientID);
        }

        public static void SetTNHSeed(int clientID, H3MP_Packet packet)
        {
            int i = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].TNHSeed = i;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                ++TNH_UIManagerPatch.seedSkip;
                Mod.currentTNHUIManager.OBS_RunSeed.SetSelectedButton(i);
                Mod.currentTNHUIManager.SetOBS_RunSeed(i);
                GM.TNHOptions.TNHSeed = i;
                --TNH_UIManagerPatch.seedSkip;
            }

            H3MP_ServerSend.SetTNHSeed(i, instance, clientID);
        }

        public static void SetTNHLevelIndex(int clientID, H3MP_Packet packet)
        {
            int levelIndex = packet.ReadInt();
            int instance = packet.ReadInt();
            
            H3MP_GameManager.TNHInstances[instance].levelIndex = levelIndex;
            
            if(Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHUIManager != null)
            {
                Mod.TNH_UIManager_m_currentLevelIndex.SetValue(Mod.currentTNHUIManager, levelIndex);
                Mod.currentTNHUIManager.CurLevelID = Mod.currentTNHUIManager.Levels[levelIndex].LevelID;
                Mod.TNH_UIManager_UpdateLevelSelectDisplayAndLoader.Invoke(Mod.currentTNHUIManager, null);
                Mod.TNH_UIManager_UpdateTableBasedOnOptions.Invoke(Mod.currentTNHUIManager, null);
                Mod.TNH_UIManager_PlayButtonSound.Invoke(Mod.currentTNHUIManager, new object[] { 2 });
            }

            H3MP_ServerSend.SetTNHLevelIndex(levelIndex, instance, clientID);
        }

        public static void SetTNHController(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int newController = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance)
            {
                if(Mod.currentTNHInstance.controller == H3MP_GameManager.ID && newController != H3MP_GameManager.ID)
                {
                    H3MP_ServerSend.TNHData(newController, Mod.currentTNHInstance.manager);

                    //++SetTNHManagerPatch.skip;
                    //Mod.currentTNHInstance.manager.enabled = false;
                    //--SetTNHManagerPatch.skip;
                }
                //else if(newController == H3MP_GameManager.ID && Mod.currentTNHInstance.controller != H3MP_GameManager.ID)
                //{
                //    ++SetTNHManagerPatch.skip;
                //    Mod.currentTNHInstance.manager.enabled = true;
                //    --SetTNHManagerPatch.skip;
                //}
            }

            H3MP_GameManager.TNHInstances[instance].controller = newController;

            H3MP_ServerSend.SetTNHController(instance, newController, clientID);
        }

        public static void TNHData(int clientID, H3MP_Packet packet)
        {
            int controller = packet.ReadInt();

            if(controller == H3MP_GameManager.ID && GM.TNH_Manager != null && Mod.currentTNHInstance != null)
            {
                H3MP_TNHData data = packet.ReadTNHData();

                if (TNH_ManagerPatch.doInit)
                {
                    Mod.initTNHData = data;
                }
                else
                {
                    Mod.InitTNHData(data);
                }
            }
            else
            {
                H3MP_ServerSend.TNHData(controller, packet);
            }
        }

        public static void TNHPlayerDied(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int ID = packet.ReadInt();

            // Process dead
            bool allDead = false;
            H3MP_TNHInstance TNHinstance = H3MP_GameManager.TNHInstances[instance];
            TNHinstance.dead.Add(ID); 
            if (TNHinstance.dead.Count >= TNHinstance.currentlyPlaying.Count)
            {
                // Set visibility of all of the previously dead players
                foreach(int playerID in TNHinstance.dead)
                {
                    if (H3MP_GameManager.players.TryGetValue(playerID, out H3MP_PlayerManager player))
                    {
                        player.SetVisible(true);
                    }
                }

                TNHinstance.Reset();
                allDead = true;
            }

            // Set player visibility if still necessary
            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentlyPlayingTNH)
            {
                if (allDead)
                {
                    GM.TNH_Manager.PlayerDied();
                }
                else
                {
                    if (H3MP_GameManager.players.TryGetValue(ID, out H3MP_PlayerManager player))
                    {
                        player.SetVisible(false);
                    }
                }
            }

            H3MP_ServerSend.TNHPlayerDied(instance, ID, clientID);
        }

        public static void TNHAddTokens(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int amount = packet.ReadInt();

            if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance currentInstance))
            {
                currentInstance.tokenCount += amount;

                // Implies we are in-game in this instance 
                if(currentInstance.manager != null && !currentInstance.dead.Contains(H3MP_GameManager.ID))
                {
                    ++TNH_ManagerPatch.addTokensSkip;
                    currentInstance.manager.AddTokens(amount, false);
                    --TNH_ManagerPatch.addTokensSkip;
                }
            }

            H3MP_ServerSend.TNHAddTokens(instance, amount, clientID);
        }

        public static void AutoMeaterSetState(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            byte state = packet.ReadByte();

            if (H3MP_Server.autoMeaters[trackedID] != null && H3MP_Server.autoMeaters[trackedID].physicalObject != null)
            {
                ++AutoMeaterSetStatePatch.skip;
                Mod.AutoMeater_SetState.Invoke(H3MP_Server.autoMeaters[trackedID].physicalObject.physicalAutoMeaterScript, new object[] { (AutoMeater.AutoMeaterState)state });
                --AutoMeaterSetStatePatch.skip;
            }

            H3MP_ServerSend.AutoMeaterSetState(trackedID, state, clientID);
        }

        public static void AutoMeaterSetBladesActive(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool active = packet.ReadBool();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[trackedID];
            if (trackedAutoMeater != null && trackedAutoMeater.physicalObject != null)
            {
                if (active)
                {
                    for (int i = 0; i < trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Blades.Count; i++)
                    {
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Blades[i].Reactivate();
                    }
                }
                else
                {
                    for (int i = 0; i < trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Blades.Count; i++)
                    {
                        trackedAutoMeater.physicalObject.physicalAutoMeaterScript.Blades[i].ShutDown();
                    }
                }
            }

            H3MP_ServerSend.AutoMeaterSetBladesActive(trackedID, active, clientID);
        }

        public static void AutoMeaterFirearmFireAtWill(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int firearmIndex = packet.ReadInt();
            bool fireAtWill = packet.ReadBool();
            float dist = packet.ReadFloat();

            H3MP_TrackedAutoMeaterData trackedAutoMeater = H3MP_Server.autoMeaters[trackedID];
            if (trackedAutoMeater != null && trackedAutoMeater.physicalObject != null)
            {
                ++AutoMeaterFirearmFireAtWillPatch.skip;
                trackedAutoMeater.physicalObject.physicalAutoMeaterScript.FireControl.Firearms[firearmIndex].SetFireAtWill(fireAtWill, dist);
                --AutoMeaterFirearmFireAtWillPatch.skip;
            }

            H3MP_ServerSend.AutoMeaterFirearmFireAtWill(trackedID, firearmIndex, fireAtWill, dist, clientID);
        }

        public static void TNHSosigKill(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int trackedID = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                H3MP_TrackedSosigData trackedSosig = H3MP_Server.sosigs[trackedID];
                if (trackedSosig != null && trackedSosig.physicalObject != null)
                {
                    ++TNH_ManagerPatch.sosigKillSkip;
                    Mod.TNH_Manager_OnSosigKill.Invoke(Mod.currentTNHInstance.manager, new object[] {trackedSosig.physicalObject.physicalSosigScript});
                    --TNH_ManagerPatch.sosigKillSkip;
                }
            }

            H3MP_ServerSend.TNHSosigKill(instance, trackedID, clientID);
        }

        public static void TNHHoldPointSystemNode(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int charIndex = packet.ReadInt();
            int progressionIndex = packet.ReadInt();
            int progressionEndlessIndex = packet.ReadInt();
            int levelIndex = packet.ReadInt();
            int holdPointIndex = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                Mod.currentTNHInstance.curHoldIndex = holdPointIndex;

                TNH_CharacterDef C = null;
                try
                {
                    C = Mod.currentTNHInstance.manager.CharDB.GetDef((TNH_Char)charIndex);
                }
                catch
                {
                    C = Mod.currentTNHInstance.manager.CharDB.GetDef(TNH_Char.DD_BeginnerBlake);
                }
                TNH_Progression currentProgression = null;
                if(progressionIndex != -1)
                {
                    currentProgression = C.Progressions[progressionIndex];
                }
                else // progressionEndlessIndex != -1
                {
                    currentProgression = C.Progressions_Endless[progressionEndlessIndex];
                }
                TNH_Progression.Level curLevel = currentProgression.Levels[levelIndex];
                TNH_HoldPoint holdPoint = Mod.currentTNHInstance.manager.HoldPoints[holdPointIndex];

                if (Mod.currentTNHInstance.holdOngoing)
                {
                    Mod.TNH_HoldPoint_CompleteHold.Invoke((TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager), null);
                    Mod.currentTNHInstance.holdOngoing = false;
                }

                Mod.TNH_Manager_m_curHoldIndex.SetValue(Mod.currentTNHInstance.manager, holdPointIndex);
                Mod.currentTNHInstance.manager.TAHReticle.DeRegisterTrackedType(TAH_ReticleContact.ContactType.Hold);
                holdPoint.ConfigureAsSystemNode(curLevel.TakeChallenge, curLevel.HoldChallenge, curLevel.NumOverrideTokensForHold);
                Mod.currentTNHInstance.manager.TAHReticle.RegisterTrackedObject(holdPoint.SpawnPoint_SystemNode, TAH_ReticleContact.ContactType.Hold);
            }
            else if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.curHoldIndex = holdPointIndex;
            }

            H3MP_ServerSend.TNHHoldPointSystemNode(instance, charIndex, progressionIndex, progressionEndlessIndex, levelIndex, holdPointIndex, clientID);
        }

        public static void TNHHoldBeginChallenge(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int barrierCount = packet.ReadInt();
            List<int> barrierIndices = new List<int>();
            List<int> barrierPrefabIndices = new List<int>();
            for(int i=0; i < barrierCount; ++i)
            {
                barrierIndices.Add(packet.ReadInt());
            }
            for(int i=0; i < barrierCount; ++i)
            {
                barrierPrefabIndices.Add(packet.ReadInt());
            }

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                Mod.currentTNHInstance.phase = TNH_Phase.Hold;
                Mod.currentTNHInstance.holdOngoing = true;
                Mod.currentTNHInstance.raisedBarriers = barrierIndices;
                Mod.currentTNHInstance.raisedBarrierPrefabIndices = barrierPrefabIndices;

                Mod.currentTNHInstance.manager.Phase = TNH_Phase.Hold;

                TNH_HoldPoint curHoldPoint = (TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager);

                // Raise barriers
                for(int i=0; i < barrierIndices.Count; ++i)
                {
                    TNH_DestructibleBarrierPoint point = curHoldPoint.BarrierPoints[barrierIndices[i]];
                    TNH_DestructibleBarrierPoint.BarrierDataSet barrierDataSet = point.BarrierDataSets[barrierPrefabIndices[i]];
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(barrierDataSet.BarrierPrefab, point.transform.position, point.transform.rotation);
                    TNH_DestructibleBarrier curBarrier = gameObject.GetComponent<TNH_DestructibleBarrier>();
                    Mod.TNH_DestructibleBarrierPoint_m_curBarrier.SetValue(point, curBarrier);
                    curBarrier.InitToPlace(point.transform.position, point.transform.forward);
                    curBarrier.SetBarrierPoint(point);
                    Mod.TNH_DestructibleBarrierPoint_SetCoverPointData.Invoke(point, new object[] { barrierPrefabIndices[i] });
                }

                // Begin hold on our side
                ++TNH_HoldPointPatch.beginHoldSkip;
                curHoldPoint.BeginHoldChallenge();
                --TNH_HoldPointPatch.beginHoldSkip;

                // If we received this it is because we are not the controller, TP to hold point
                GM.CurrentMovementManager.TeleportToPoint(curHoldPoint.SpawnPoint_SystemNode.position, true);
            }
            else if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.phase = TNH_Phase.Hold;
                actualInstance.holdOngoing = true;
                actualInstance.raisedBarriers = barrierIndices;
                actualInstance.raisedBarrierPrefabIndices = barrierPrefabIndices;
            }

            H3MP_ServerSend.TNHHoldBeginChallenge(instance, barrierIndices, barrierPrefabIndices, clientID);
        }

        public static void ShatterableCrateDamage(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_Server.items[trackedID] != null && H3MP_Server.items[trackedID].controller == H3MP_GameManager.ID)
            {
                ++TNH_ShatterableCrateDamagePatch.skip;
                H3MP_Server.items[trackedID].physicalItem.GetComponent<TNH_ShatterableCrate>().Damage(packet.ReadDamage());
                --TNH_ShatterableCrateDamagePatch.skip;
            }
            else
            {
                H3MP_ServerSend.ShatterableCrateDamage(trackedID, packet.ReadDamage());
            }
        }

        public static void ShatterableCrateDestroy(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();

            if (H3MP_Server.items[trackedID] != null && H3MP_Server.items[trackedID].physicalItem != null)
            {
                ++TNH_ShatterableCrateDestroyPatch.skip;
                Mod.TNH_ShatterableCrate_Destroy.Invoke(H3MP_Server.items[trackedID].physicalItem.GetComponent<TNH_ShatterableCrate>(), new object[] { packet.ReadDamage() });
                --TNH_ShatterableCrateDestroyPatch.skip;
            }

            H3MP_ServerSend.ShatterableCrateDestroy(trackedID, packet.ReadDamage(), clientID);
        }

        public static void TNHSetLevel(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int level = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                Mod.currentTNHInstance.level = level;
                Mod.TNH_Manager_m_level.SetValue(Mod.currentTNHInstance.manager, level);
                Mod.TNH_Manager_SetLevel.Invoke(Mod.currentTNHInstance.manager, new object[] { level });
            }
            else if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.level = level;
            }

            H3MP_ServerSend.TNHSetLevel(instance, level, clientID);
        }

        public static void TNHSetPhaseTake(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            int activeSupplyCount = packet.ReadInt();
            List<int> activeIndices = new List<int>();
            for(int i=0; i < activeSupplyCount; ++i)
            {
                activeIndices.Add(packet.ReadInt());
            }
            List<TNH_SupplyPoint.SupplyPanelType> types = new List<TNH_SupplyPoint.SupplyPanelType>();
            for(int i=0; i < activeSupplyCount; ++i)
            {
                types.Add((TNH_SupplyPoint.SupplyPanelType)packet.ReadByte());
            }

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                Mod.currentTNHInstance.phase = TNH_Phase.Take;
                Mod.currentTNHInstance.activeSupplyPointIndices = activeIndices;
                Mod.currentTNHInstance.supplyPanelTypes = types;

                Mod.TNH_Manager_SetPhase_Take.Invoke(Mod.currentTNHInstance.manager, null);
            }
            else if(H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.phase = TNH_Phase.Take;
                actualInstance.activeSupplyPointIndices = activeIndices;
                actualInstance.supplyPanelTypes = types;
            }

            H3MP_ServerSend.TNHSetPhaseTake(instance, activeIndices, types, clientID);
        }

        public static void TNHHoldCompletePhase(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                Mod.TNH_HoldPoint_CompletePhase.Invoke(Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager), null);
            }

            H3MP_ServerSend.TNHHoldCompletePhase(instance, clientID);
        }

        public static void TNHHoldShutDown(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                ((TNH_HoldPoint)Mod.TNH_Manager_m_curHoldPoint.GetValue(Mod.currentTNHInstance.manager)).ShutDownHoldPoint();
            }

            H3MP_ServerSend.TNHHoldShutDown(instance, clientID);
        }

        public static void TNHSetPhaseComplete(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();

            if (Mod.currentTNHInstance != null && Mod.currentTNHInstance.instance == instance && Mod.currentTNHInstance.manager != null)
            {
                Mod.TNH_Manager_SetPhase_Completed.Invoke(Mod.currentTNHInstance.manager, null);
            }

            H3MP_ServerSend.TNHSetPhaseComplete(instance, clientID);
        }

        public static void TNHSetPhase(int clientID, H3MP_Packet packet)
        {
            int instance = packet.ReadInt();
            short p = packet.ReadShort();

            if (H3MP_GameManager.TNHInstances.TryGetValue(instance, out H3MP_TNHInstance actualInstance))
            {
                actualInstance.phase = (TNH_Phase)p;
            }

            H3MP_ServerSend.TNHSetPhase(instance, p, clientID);
        }

        public static void EncryptionRespawnSubTarg(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            if (H3MP_Server.encryptions[trackedID] != null && H3MP_Server.encryptions[trackedID].physicalObject != null)
            {
                H3MP_Server.encryptions[trackedID].subTargsActive[index] = true;

                H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript.SubTargs[index].SetActive(true);
                Mod.TNH_EncryptionTarget_m_numSubTargsLeft.SetValue(H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript, (int)Mod.TNH_EncryptionTarget_m_numSubTargsLeft.GetValue(H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript));
            }

            H3MP_ServerSend.EncryptionRespawnSubTarg(trackedID, index, clientID);
        }

        public static void EncryptionSpawnGrowth(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Vector3 point = packet.ReadVector3();

            if (H3MP_Server.encryptions[trackedID] != null && H3MP_Server.encryptions[trackedID].physicalObject != null)
            {
                H3MP_Server.encryptions[trackedID].tendrilsActive[index] = true;
                H3MP_Server.encryptions[trackedID].growthPoints[index] = point;
                H3MP_Server.encryptions[trackedID].subTargsPos[index] = point;
                H3MP_Server.encryptions[trackedID].subTargsActive[index] = true;
                H3MP_Server.encryptions[trackedID].tendrilFloats[index] = 1f;
                Vector3 forward = point - H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript.Tendrils[index].transform.position;
                H3MP_Server.encryptions[trackedID].tendrilsRot[index] = Quaternion.LookRotation(forward);
                H3MP_Server.encryptions[trackedID].tendrilsScale[index] = new Vector3(0.2f, 0.2f, forward.magnitude);

                ++EncryptionSpawnGrowthPatch.skip;
                Mod.TNH_EncryptionTarget_SpawnGrowth.Invoke(H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript, new object[] { index, point});
                --EncryptionSpawnGrowthPatch.skip;
            }

            H3MP_ServerSend.EncryptionSpawnGrowth(trackedID, index, point, clientID);
        }

        public static void EncryptionRecursiveInit(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int count = packet.ReadInt();
            List<int> indices = new List<int>();
            for(int i=0; i < count; i++)
            {
                indices.Add(packet.ReadInt());
            }

            if (H3MP_Server.encryptions[trackedID] != null && H3MP_Server.encryptions[trackedID].physicalObject != null)
            {
                for(int i=0; i < count; ++i)
                {
                    H3MP_Server.encryptions[trackedID].subTargsActive[indices[i]] = true;
                    H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript.SubTargs[indices[i]].SetActive(true);
                }
                Mod.TNH_EncryptionTarget_m_numSubTargsLeft.SetValue(H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript, count);
            }

            H3MP_ServerSend.EncryptionRecursiveInit(trackedID, indices, clientID);
        }

        public static void EncryptionResetGrowth(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Vector3 point = packet.ReadVector3();

            if (H3MP_Server.encryptions[trackedID] != null && H3MP_Server.encryptions[trackedID].physicalObject != null)
            {
                H3MP_Server.encryptions[trackedID].growthPoints[index] = point;
                H3MP_Server.encryptions[trackedID].tendrilFloats[index] = 0;
                Vector3 forward = point - H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript.Tendrils[index].transform.position;
                H3MP_Server.encryptions[trackedID].tendrilsRot[index] = Quaternion.LookRotation(forward);
                H3MP_Server.encryptions[trackedID].tendrilsScale[index] = new Vector3(0.2f, 0.2f, forward.magnitude);

                ++EncryptionResetGrowthPatch.skip;
                Mod.TNH_EncryptionTarget_ResetGrowth.Invoke(H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript, new object[] {index, point});
                --EncryptionResetGrowthPatch.skip;
            }

            H3MP_ServerSend.EncryptionResetGrowth(trackedID, index, point, clientID);
        }

        public static void EncryptionDisableSubtarg(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();

            if (H3MP_Server.encryptions[trackedID] != null && H3MP_Server.encryptions[trackedID].physicalObject != null)
            {
                H3MP_Server.encryptions[trackedID].subTargsActive[index] = false;

                H3MP_Server.encryptions[trackedID].physicalObject.physicalEncryptionScript.SubTargs[index].SetActive(false);
            }

            H3MP_ServerSend.EncryptionDisableSubtarg(trackedID, index, clientID);
        }

        public static void EncryptionSubDamage(int clientID, H3MP_Packet packet)
        {
            int trackedID = packet.ReadInt();
            int index = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedEncryptionData trackedEncryption = H3MP_Server.encryptions[trackedID];
            if (trackedEncryption != null)
            {
                if (trackedEncryption.controller == 0)
                {
                    if (trackedEncryption.physicalObject != null)
                    {
                        ++EncryptionSubDamagePatch.skip;
                        trackedEncryption.physicalObject.physicalEncryptionScript.SubTargs[index].GetComponent<TNH_EncryptionTarget_SubTarget>().Damage(damage);
                        --EncryptionSubDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.EncryptionSubDamage(trackedEncryption, index, damage);
                }
            }
        }

        public static void SosigWeaponDamage(int clientID, H3MP_Packet packet)
        {
            int sosigWeaponTrackedID = packet.ReadInt();
            Damage damage = packet.ReadDamage();

            H3MP_TrackedItemData trackedItem = H3MP_Client.items[sosigWeaponTrackedID];
            if (trackedItem != null)
            {
                if (trackedItem.controller == H3MP_GameManager.ID)
                {
                    if (trackedItem.physicalItem != null)
                    {
                        ++SosigWeaponDamagePatch.skip;
                        (trackedItem.physicalItem.physicalObject as SosigWeaponPlayerInterface).W.Damage(damage);
                        --SosigWeaponDamagePatch.skip;
                    }
                }
                else
                {
                    H3MP_ServerSend.SosigWeaponDamage(trackedItem, damage);
                }
            }
        }
    }
}
