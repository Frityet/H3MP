﻿using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;
using static Valve.VR.SteamVR_ExternalCamera;

namespace H3MP
{
    // MOSTLY WRITTEN BY Tom Weiland: https://tomweiland.net/github

    /// <summary>Sent from server to client.</summary>
    public enum ServerPackets
    {
        welcome = 1,
        spawnPlayer = 2,
        playerState = 3,
        playerScene = 4,
        addSyncScene = 5,
        trackedItems = 6,
        trackedItem = 7,
        takeControl = 8,
        giveControl = 9,
        destroyItem = 10,
        itemParent = 11,
        connectSync = 12,
        weaponFire = 13,
        playerDamage = 14,
        trackedSosig = 15,
        trackedSosigs = 16,
        giveSosigControl = 17,
        destroySosig = 18,
        sosigPickUpItem = 19,
        sosigPlaceItemIn = 20,
        sosigDropSlot = 21,
        sosigHandDrop = 22,
        sosigConfigure = 23,
        sosigLinkRegisterWearable = 24,
        sosigLinkDeRegisterWearable = 25,
        sosigSetIFF = 26,
        sosigSetOriginalIFF = 27,
        sosigLinkDamage = 28,
        sosigDamageData = 29,
        sosigWearableDamage = 30
    }

    /// <summary>Sent from client to server.</summary>
    public enum ClientPackets
    {
        welcomeReceived = 1,
        playerState = 2,
        playerScene = 3,
        addSyncScene = 4,
        trackedItems = 5,
        trackedItem = 6,
        takeControl = 7,
        giveControl = 8,
        destroyItem = 9,
        itemParent = 10,
        weaponFire = 11,
        playerDamage = 12,
        trackedSosig = 13,
        trackedSosigs = 14,
        giveSosigControl = 15,
        destroySosig = 16,
        sosigPickupItem = 17,
        sosigPlaceItemIn = 18,
        sosigDropSlot = 19,
        sosigHandDrop = 20,
        sosigConfigure = 21,
        sosigLinkRegisterWearable = 22,
        sosigLinkDeRegisterWearable = 23,
        sosigSetIFF = 24,
        sosigSetOriginalIFF = 25,
        sosigLinkDamage = 26,
        sosigDamageData = 27,
        sosigWearableDamage = 28
    }

    public class H3MP_Packet : IDisposable
    {
        public List<byte> buffer;
        public byte[] readableBuffer;
        public int readPos;

        /// <summary>Creates a new empty packet (without an ID).</summary>
        public H3MP_Packet()
        {
            buffer = new List<byte>(); // Intitialize buffer
            readPos = 0; // Set readPos to 0
        }

        /// <summary>Creates a new packet with a given ID. Used for sending.</summary>
        /// <param name="_id">The packet ID.</param>
        public H3MP_Packet(int _id)
        {
            buffer = new List<byte>(); // Intitialize buffer
            readPos = 0; // Set readPos to 0

            Write(_id); // Write packet id to the buffer
        }

        /// <summary>Creates a packet from which data can be read. Used for receiving.</summary>
        /// <param name="_data">The bytes to add to the packet.</param>
        public H3MP_Packet(byte[] _data)
        {
            buffer = new List<byte>(); // Intitialize buffer
            readPos = 0; // Set readPos to 0

            SetBytes(_data);
        }

        #region Functions
        /// <summary>Sets the packet's content and prepares it to be read.</summary>
        /// <param name="_data">The bytes to add to the packet.</param>
        public void SetBytes(byte[] _data)
        {
            Write(_data);
            readableBuffer = buffer.ToArray();
        }

        /// <summary>Inserts the length of the packet's content at the start of the buffer.</summary>
        public void WriteLength()
        {
            buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count)); // Insert the byte length of the packet at the very beginning
        }

        /// <summary>Inserts the given int at the start of the buffer.</summary>
        /// <param name="_value">The int to insert.</param>
        public void InsertInt(int _value)
        {
            buffer.InsertRange(0, BitConverter.GetBytes(_value)); // Insert the int at the start of the buffer
        }

        /// <summary>Gets the packet's content in array form.</summary>
        public byte[] ToArray()
        {
            readableBuffer = buffer.ToArray();
            return readableBuffer;
        }

        /// <summary>Gets the length of the packet's content.</summary>
        public int Length()
        {
            return buffer.Count; // Return the length of buffer
        }

        /// <summary>Gets the length of the unread data contained in the packet.</summary>
        public int UnreadLength()
        {
            return Length() - readPos; // Return the remaining length (unread)
        }

        /// <summary>Resets the packet instance to allow it to be reused.</summary>
        /// <param name="_shouldReset">Whether or not to reset the packet.</param>
        public void Reset(bool _shouldReset = true)
        {
            if (_shouldReset)
            {
                buffer.Clear(); // Clear buffer
                readableBuffer = null;
                readPos = 0; // Reset readPos
            }
            else
            {
                readPos -= 4; // "Unread" the last read int
            }
        }
        #endregion

        #region Write Data
        /// <summary>Adds a byte to the packet.</summary>
        /// <param name="_value">The byte to add.</param>
        public void Write(byte _value)
        {
            buffer.Add(_value);
        }
        /// <summary>Adds an array of bytes to the packet.</summary>
        /// <param name="_value">The byte array to add.</param>
        public void Write(byte[] _value)
        {
            buffer.AddRange(_value);
        }
        /// <summary>Adds a short to the packet.</summary>
        /// <param name="_value">The short to add.</param>
        public void Write(short _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds an int to the packet.</summary>
        /// <param name="_value">The int to add.</param>
        public void Write(int _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds a long to the packet.</summary>
        /// <param name="_value">The long to add.</param>
        public void Write(long _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds a float to the packet.</summary>
        /// <param name="_value">The float to add.</param>
        public void Write(float _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds a bool to the packet.</summary>
        /// <param name="_value">The bool to add.</param>
        public void Write(bool _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds a string to the packet.</summary>
        /// <param name="_value">The string to add.</param>
        public void Write(string _value)
        {
            Write(_value.Length); // Add the length of the string to the packet
            buffer.AddRange(Encoding.ASCII.GetBytes(_value)); // Add the string itself
        }
        /// <summary>Adds a Vector3 to the packet.</summary>
        /// <param name="_value">The Vector3 to add.</param>
        public void Write(Vector3 _value)
        {
            Write(_value.x);
            Write(_value.y);
            Write(_value.z);
        }
        /// <summary>Adds a Vector2 to the packet.</summary>
        /// <param name="_value">The Vector2 to add.</param>
        public void Write(Vector2 _value)
        {
            Write(_value.x);
            Write(_value.y);
        }
        /// <summary>Adds a Quaternion to the packet.</summary>
        /// <param name="_value">The Quaternion to add.</param>
        public void Write(Quaternion _value)
        {
            Write(_value.x);
            Write(_value.y);
            Write(_value.z);
            Write(_value.w);
        }
        /// <summary>Adds a H3MP_TrackedItemData to the packet.</summary>
        /// <param name="_value">The H3MP_TrackedItemData to add.</param>
        public void Write(H3MP_TrackedItemData trackedItem, bool full = false)
        {
            Write(trackedItem.trackedID);
            Write(trackedItem.position);
            Write(trackedItem.rotation);
            if(trackedItem.data == null || trackedItem.data.Length == 0)
            {
                Write(0);
            }
            else
            {
                Write(trackedItem.data.Length);
                Write(trackedItem.data);
            }
            Write(trackedItem.active);
            //TODO Make which data we send dependent on what updated, cause we shouldn't send the entire data array if there was no update in it
            if (full)
            {
                Write(trackedItem.itemID);
                Write(trackedItem.controller);
                Write(trackedItem.parent);
                Write(trackedItem.localTrackedID);
            }
            else
            {
                Write(trackedItem.order++);
            }
        }
        /// <summary>Adds a Damage to the packet.</summary>
        /// <param name="_value">The Damage to add.</param>
        public void Write(Damage damage)
        {
            Write(damage.point);
            Write(damage.Source_IFF);
            Write(damage.Source_Point);
            Write(damage.Dam_Blunt);
            Write(damage.Dam_Piercing);
            Write(damage.Dam_Cutting);
            Write(damage.Dam_TotalKinetic);
            Write(damage.Dam_Thermal);
            Write(damage.Dam_Chilling);
            Write(damage.Dam_EMP);
            Write(damage.Dam_TotalEnergetic);
            Write(damage.Dam_Stunning);
            Write(damage.Dam_Blinding);
            Write(damage.hitNormal);
            Write(damage.strikeDir);
            Write(damage.edgeNormal);
            Write(damage.damageSize);
            Write((byte)damage.Class);
        }
        /// <summary>Adds a H3MP_TrackedSosigData to the packet.</summary>
        /// <param name="trackedSosig">The H3MP_TrackedSosigData to add.</param>
        /// <param name="full">Whether to include all necessary data to instantiate this sosig.</param>
        public void Write(H3MP_TrackedSosigData trackedSosig, bool full = false)
        {
            Write(trackedSosig.trackedID);
            Write(trackedSosig.position);
            Write(trackedSosig.rotation);
            Write(trackedSosig.active);
            if(trackedSosig.ammoStores != null && trackedSosig.ammoStores.Length > 0)
            {
                Write((byte)trackedSosig.ammoStores.Length);
                for(int i=0; i < trackedSosig.ammoStores.Length; ++i)
                {
                    Write(trackedSosig.ammoStores[i]);
                }
            }
            else
            {
                Write((byte)0);
            }
            if(trackedSosig.linkData == null || trackedSosig.linkData.Length == 0)
            {
                Write((byte)0);
            }
            else
            {
                Write((byte)trackedSosig.linkData.Length);
                for (int i = 0; i < trackedSosig.linkData.Length; ++i) 
                {
                    for(int k=0; k < 5; ++k)
                    {
                        Write(trackedSosig.linkData[i][k]);
                    }
                }
            }

            if (full)
            {
                Write((byte)trackedSosig.IFF);
                Write(trackedSosig.configTemplate);
                Write(trackedSosig.controller);
                Write(trackedSosig.localTrackedID);
                Write((byte)trackedSosig.wearables.Count);
                for(int i=0; i < trackedSosig.wearables.Count; ++i)
                {
                    if (trackedSosig.wearables[i] == null || trackedSosig.wearables[i].Count == 0)
                    {
                        Write((byte)0);
                    }
                    else
                    {
                        Write((byte)trackedSosig.wearables[i].Count);
                        for (int j = 0; j < trackedSosig.wearables[i].Count; ++j)
                        {
                            Write(trackedSosig.wearables[i][j]);
                        }
                    }
                }
            }
            else
            {
                Write(trackedSosig.order++);
            }
        }
        /// <summary>Adds a SosigConfigTemplate to the packet.</summary>
        /// <param name="config">The SosigConfigTemplate to add.</param>
        public void Write(SosigConfigTemplate config)
        {
            Write(config.AppliesDamageResistToIntegrityLoss);
            Write(config.DoesDropWeaponsOnBallistic);
            Write(config.TotalMustard);
            Write(config.BleedDamageMult);
            Write(config.BleedRateMultiplier);
            Write(config.BleedVFXIntensity);
            Write(config.SearchExtentsModifier);
            Write(config.ShudderThreshold);
            Write(config.ConfusionThreshold);
            Write(config.ConfusionMultiplier);
            Write(config.ConfusionTimeMax);
            Write(config.StunThreshold);
            Write(config.StunMultiplier);
            Write(config.StunTimeMax);
            Write(config.HasABrain);
            Write(config.RegistersPassiveThreats);
            Write(config.CanBeKnockedOut);
            Write(config.MaxUnconsciousTime);
            Write(config.AssaultPointOverridesSkirmishPointWhenFurtherThan);
            Write(config.ViewDistance);
            Write(config.HearingDistance);
            Write(config.MaxFOV);
            Write(config.StateSightRangeMults);
            Write(config.StateHearingRangeMults);
            Write(config.StateFOVMults);
            Write(config.CanPickup_Ranged);
            Write(config.CanPickup_Melee);
            Write(config.CanPickup_Other);
            Write(config.DoesJointBreakKill_Head);
            Write(config.DoesJointBreakKill_Upper);
            Write(config.DoesJointBreakKill_Lower);
            Write(config.DoesSeverKill_Head);
            Write(config.DoesSeverKill_Upper);
            Write(config.DoesSeverKill_Lower);
            Write(config.DoesExplodeKill_Head);
            Write(config.DoesExplodeKill_Upper);
            Write(config.DoesExplodeKill_Lower);
            Write(config.CrawlSpeed);
            Write(config.SneakSpeed);
            Write(config.WalkSpeed);
            Write(config.RunSpeed);
            Write(config.TurnSpeed);
            Write(config.MovementRotMagnitude);
            Write(config.DamMult_Projectile);
            Write(config.DamMult_Explosive);
            Write(config.DamMult_Melee);
            Write(config.DamMult_Piercing);
            Write(config.DamMult_Blunt);
            Write(config.DamMult_Cutting);
            Write(config.DamMult_Thermal);
            Write(config.DamMult_Chilling);
            Write(config.DamMult_EMP);
            Write(config.CanBeSurpressed);
            Write(config.SuppressionMult);
            Write(config.CanBeGrabbed);
            Write(config.CanBeSevered);
            Write(config.CanBeStabbed);
            Write(config.MaxJointLimit);
            if(config.LinkDamageMultipliers == null || config.LinkDamageMultipliers.Count == 0)
            {
                Write((byte)0);
            }
            else
            {
                Write((byte)config.LinkDamageMultipliers.Count);
                foreach(float f in config.LinkDamageMultipliers)
                {
                    Write(f);
                }
            }
            if(config.LinkStaggerMultipliers == null || config.LinkStaggerMultipliers.Count == 0)
            {
                Write((byte)0);
            }
            else
            {
                Write((byte)config.LinkStaggerMultipliers.Count);
                foreach(float f in config.LinkStaggerMultipliers)
                {
                    Write(f);
                }
            }
            if(config.StartingLinkIntegrity == null || config.StartingLinkIntegrity.Count == 0)
            {
                Write((byte)0);
            }
            else
            {
                Write((byte)config.StartingLinkIntegrity.Count);
                foreach(Vector2 v in config.StartingLinkIntegrity)
                {
                    Write(v);
                }
            }
            if (config.StartingChanceBrokenJoint == null || config.StartingChanceBrokenJoint.Count == 0)
            {
                Write((byte)0);
            }
            else
            {
                Write((byte)config.StartingChanceBrokenJoint.Count);
                foreach (float f in config.StartingChanceBrokenJoint)
                {
                    Write(f);
                }
            }
            Write(config.TargetCapacity);
            Write(config.TargetTrackingTime);
            Write(config.NoFreshTargetTime);
        }
        #endregion

        #region Read Data
        /// <summary>Reads a byte from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public byte ReadByte(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                byte _value = readableBuffer[readPos]; // Get the byte at readPos' position
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 1; // Increase readPos by 1
                }
                return _value; // Return the byte
            }
            else
            {
                throw new Exception("Could not read value of type 'byte'!");
            }
        }

        /// <summary>Reads an array of bytes from the packet.</summary>
        /// <param name="_length">The length of the byte array.</param>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public byte[] ReadBytes(int _length, bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                byte[] _value = buffer.GetRange(readPos, _length).ToArray(); // Get the bytes at readPos' position with a range of _length
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += _length; // Increase readPos by _length
                }
                return _value; // Return the bytes
            }
            else
            {
                throw new Exception("Could not read value of type 'byte[]'!");
            }
        }

        /// <summary>Reads a short from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public short ReadShort(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                short _value = BitConverter.ToInt16(readableBuffer, readPos); // Convert the bytes to a short
                if (_moveReadPos)
                {
                    // If _moveReadPos is true and there are unread bytes
                    readPos += 2; // Increase readPos by 2
                }
                return _value; // Return the short
            }
            else
            {
                throw new Exception("Could not read value of type 'short'!");
            }
        }

        /// <summary>Reads an int from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public int ReadInt(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                int _value = BitConverter.ToInt32(readableBuffer, readPos); // Convert the bytes to an int
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 4; // Increase readPos by 4
                }
                return _value; // Return the int
            }
            else
            {
                throw new Exception("Could not read value of type 'int'!");
            }
        }

        /// <summary>Reads a long from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public long ReadLong(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                long _value = BitConverter.ToInt64(readableBuffer, readPos); // Convert the bytes to a long
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 8; // Increase readPos by 8
                }
                return _value; // Return the long
            }
            else
            {
                throw new Exception("Could not read value of type 'long'!");
            }
        }

        /// <summary>Reads a float from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public float ReadFloat(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                float _value = BitConverter.ToSingle(readableBuffer, readPos); // Convert the bytes to a float
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 4; // Increase readPos by 4
                }
                return _value; // Return the float
            }
            else
            {
                throw new Exception("Could not read value of type 'float'!");
            }
        }

        /// <summary>Reads a bool from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public bool ReadBool(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                bool _value = BitConverter.ToBoolean(readableBuffer, readPos); // Convert the bytes to a bool
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 1; // Increase readPos by 1
                }
                return _value; // Return the bool
            }
            else
            {
                throw new Exception("Could not read value of type 'bool'!");
            }
        }

        /// <summary>Reads a string from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public string ReadString(bool _moveReadPos = true)
        {
            try
            {
                int _length = ReadInt(); // Get the length of the string
                string _value = Encoding.ASCII.GetString(readableBuffer, readPos, _length); // Convert the bytes to a string
                if (_moveReadPos && _value.Length > 0)
                {
                    // If _moveReadPos is true string is not empty
                    readPos += _length; // Increase readPos by the length of the string
                }
                return _value; // Return the string
            }
            catch
            {
                throw new Exception("Could not read value of type 'string'!");
            }
        }

        /// <summary>Reads a Vector3 from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public Vector3 ReadVector3(bool _moveReadPos = true)
        {
            return new Vector3(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
        }

        /// <summary>Reads a Vector2 from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public Vector2 ReadVector2(bool _moveReadPos = true)
        {
            return new Vector2(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
        }

        /// <summary>Reads a Quaternion from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public Quaternion ReadQuaternion(bool _moveReadPos = true)
        {
            return new Quaternion(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
        }

        /// <summary>Reads a H3MP_TrackedItemData from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public H3MP_TrackedItemData ReadTrackedItem(bool full = false, bool _moveReadPos = true)
        {
            H3MP_TrackedItemData trackedItem = new H3MP_TrackedItemData();
            trackedItem.trackedID = ReadInt();
            trackedItem.position = ReadVector3();
            trackedItem.rotation = ReadQuaternion();
            int dataLength = ReadInt();
            if (dataLength > 0)
            {
                trackedItem.data = ReadBytes(dataLength);
            }
            trackedItem.active = ReadBool();

            if (full)
            {
                trackedItem.itemID = ReadString();
                trackedItem.controller = ReadInt();
                trackedItem.parent = ReadInt();
                trackedItem.localTrackedID = ReadInt();
            }
            else
            {
                trackedItem.order = ReadByte();
            }

            return trackedItem;
        }

        /// <summary>Reads a H3MP_TrackedSosigData from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public H3MP_TrackedSosigData ReadTrackedSosig(bool full = false, bool _moveReadPos = true)
        {
            H3MP_TrackedSosigData trackedSosig = new H3MP_TrackedSosigData();

            trackedSosig.trackedID = ReadInt();
            trackedSosig.position = ReadVector3();
            trackedSosig.rotation = ReadQuaternion();
            trackedSosig.active = ReadBool();
            byte ammoStoreLength = ReadByte();
            if(ammoStoreLength > 0)
            {
                trackedSosig.ammoStores = new int[ammoStoreLength];
                for(int i=0; i < ammoStoreLength; ++i)
                {
                    trackedSosig.ammoStores[i] = ReadInt();
                }
            }
            byte sosigLinkDataLength = ReadByte();
            if (sosigLinkDataLength > 0)
            {
                if(trackedSosig.linkData == null)
                {
                    trackedSosig.linkData = new float[sosigLinkDataLength][];
                }
                for (int i = 0; i < sosigLinkDataLength; ++i)
                {
                    if(trackedSosig.linkData[i] == null || trackedSosig.linkData[i].Length != 5)
                    {
                        trackedSosig.linkData[i] = new float[5];
                    }

                    for (int j = 0; j < 5; ++j)
                    {
                        trackedSosig.linkData[i][j] = ReadFloat();
                    }
                }
            }

            if (full)
            {
                trackedSosig.IFF = ReadByte();
                trackedSosig.configTemplate = ReadSosigConfig();
                trackedSosig.controller = ReadInt();
                trackedSosig.localTrackedID = ReadInt();
                byte linkCount = ReadByte();
                trackedSosig.wearables = new List<List<string>>();
                for(int i=0; i < linkCount; ++i)
                {
                    trackedSosig.wearables.Add(new List<string>());
                    byte wearableCount = ReadByte();
                    if(wearableCount > 0)
                    {
                        for(int j = 0; j < wearableCount; ++j)
                        {
                            trackedSosig.wearables[i].Add(ReadString());
                        }
                    }
                }
            }
            else
            {
                trackedSosig.order = ReadByte();
            }

            return trackedSosig;
        }

        /// <summary>Reads a SosigConfigTemplate from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public SosigConfigTemplate ReadSosigConfig(bool full = false, bool _moveReadPos = true)
        {
            SosigConfigTemplate config = ScriptableObject.CreateInstance<SosigConfigTemplate>();

            config.AppliesDamageResistToIntegrityLoss = ReadBool();
            config.DoesDropWeaponsOnBallistic = ReadBool();
            config.TotalMustard = ReadFloat();
            config.BleedDamageMult = ReadFloat();
            config.BleedRateMultiplier = ReadFloat();
            config.BleedVFXIntensity = ReadFloat();
            config.SearchExtentsModifier = ReadFloat();
            config.ShudderThreshold = ReadFloat();
            config.ConfusionThreshold = ReadFloat();
            config.ConfusionMultiplier = ReadFloat();
            config.ConfusionTimeMax = ReadFloat();
            config.StunThreshold = ReadFloat();
            config.StunMultiplier = ReadFloat();
            config.StunTimeMax = ReadFloat();
            config.HasABrain = ReadBool();
            config.RegistersPassiveThreats = ReadBool();
            config.CanBeKnockedOut = ReadBool();
            config.MaxUnconsciousTime = ReadFloat();
            config.AssaultPointOverridesSkirmishPointWhenFurtherThan = ReadFloat();
            config.ViewDistance = ReadFloat();
            config.HearingDistance = ReadFloat();
            config.MaxFOV = ReadFloat();
            config.StateSightRangeMults = ReadVector3();
            config.StateHearingRangeMults = ReadVector3();
            config.StateFOVMults = ReadVector3();
            config.CanPickup_Ranged = ReadBool();
            config.CanPickup_Melee = ReadBool();
            config.CanPickup_Other = ReadBool();
            config.DoesJointBreakKill_Head = ReadBool();
            config.DoesJointBreakKill_Upper = ReadBool();
            config.DoesJointBreakKill_Lower = ReadBool();
            config.DoesSeverKill_Head = ReadBool();
            config.DoesSeverKill_Upper = ReadBool();
            config.DoesSeverKill_Lower = ReadBool();
            config.DoesExplodeKill_Head = ReadBool();
            config.DoesExplodeKill_Upper = ReadBool();
            config.DoesExplodeKill_Lower = ReadBool();
            config.CrawlSpeed = ReadFloat();
            config.SneakSpeed = ReadFloat();
            config.WalkSpeed = ReadFloat();
            config.RunSpeed = ReadFloat();
            config.TurnSpeed = ReadFloat();
            config.MovementRotMagnitude = ReadFloat();
            config.DamMult_Projectile = ReadFloat();
            config.DamMult_Explosive = ReadFloat();
            config.DamMult_Melee = ReadFloat();
            config.DamMult_Piercing = ReadFloat();
            config.DamMult_Blunt = ReadFloat();
            config.DamMult_Cutting = ReadFloat();
            config.DamMult_Thermal = ReadFloat();
            config.DamMult_Chilling = ReadFloat();
            config.DamMult_EMP = ReadFloat();
            config.CanBeSurpressed = ReadBool();
            config.SuppressionMult = ReadFloat();
            config.CanBeGrabbed = ReadBool();
            config.CanBeSevered = ReadBool();
            config.CanBeStabbed = ReadBool();
            config.MaxJointLimit = ReadFloat();
            byte linkDamMultCount = ReadByte();
            if (linkDamMultCount > 0)
            {
                config.LinkDamageMultipliers = new List<float>();
                for (int i=0; i < linkDamMultCount; ++i)
                {
                    config.LinkDamageMultipliers.Add(ReadFloat());
                }
            }
            byte linkStaggerMultCount = ReadByte();
            if (linkStaggerMultCount > 0)
            {
                config.LinkStaggerMultipliers = new List<float>();
                for (int i = 0; i < linkStaggerMultCount; ++i)
                {
                    config.LinkStaggerMultipliers.Add(ReadFloat());
                }
            }
            byte startLinkIntegCount = ReadByte();
            if (startLinkIntegCount > 0)
            {
                config.StartingLinkIntegrity = new List<Vector2>();
                for (int i = 0; i < startLinkIntegCount; ++i)
                {
                    config.StartingLinkIntegrity.Add(ReadVector2());
                }
            }
            byte startBreakChanceCount = ReadByte();
            if (startBreakChanceCount > 0)
            {
                config.StartingChanceBrokenJoint = new List<float>();
                for (int i = 0; i < startBreakChanceCount; ++i)
                {
                    config.StartingChanceBrokenJoint.Add(ReadFloat());
                }
            }
            config.TargetCapacity = ReadInt();
            config.TargetTrackingTime = ReadFloat();
            config.NoFreshTargetTime = ReadFloat();

            return config;
        }

        /// <summary>Reads a Damage from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public Damage ReadDamage(bool _moveReadPos = true)
        {
            Damage damage = new Damage();

            damage.point = ReadVector3();
            damage.Source_IFF = ReadInt();
            damage.Source_Point = ReadVector3();
            damage.Dam_Blunt = ReadFloat();
            damage.Dam_Piercing = ReadFloat();
            damage.Dam_Cutting = ReadFloat();
            damage.Dam_TotalKinetic = ReadFloat();
            damage.Dam_Thermal = ReadFloat();
            damage.Dam_Chilling = ReadFloat();
            damage.Dam_EMP = ReadFloat();
            damage.Dam_TotalEnergetic = ReadFloat();
            damage.Dam_Stunning = ReadFloat();
            damage.Dam_Blinding = ReadFloat();
            damage.hitNormal = ReadVector3();
            damage.strikeDir = ReadVector3();
            damage.edgeNormal = ReadVector3();
            damage.damageSize = ReadFloat();
            damage.Class = (Damage.DamageClass)ReadByte();

            return damage;
        }
        #endregion

        private bool disposed = false;

        protected virtual void Dispose(bool _disposing)
        {
            if (!disposed)
            {
                if (_disposing)
                {
                    buffer = null;
                    readableBuffer = null;
                    readPos = 0;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}