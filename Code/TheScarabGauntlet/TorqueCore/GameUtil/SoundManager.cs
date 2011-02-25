//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.XNA;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.GameUtil
{
    /// <summary>
    /// Structure that contains a SoundBank and a WaveBank. Intended to simplify the use of the ridiculous XACT tools.
    /// SoundGroups will most likely be used only by the SoundManager, but it's possible to use them alone, if neccesary.
    /// The benefit of using the SoundManager is that it will automatically create and hash SoundGroups for every sound
    /// you use, so if you use it properly you are guaranteed to have only one instance of a given sound bank.
    /// It's reccomended that you read the XACT docs before trying to use any kind of sound in XNA.
    /// </summary>
    public class SoundGroup
    {

        #region Constructors

        /// <summary>
        /// Attempt to create a new SoundGroup with the specified SoundBank and WaveBank
        /// </summary>
        /// <param name="soundBankPath">The path to the .xsb file (with extention).</param>
        /// <param name="waveBankPath">The path to the .xwb file (with extention).</param>
        public SoundGroup(string soundBankPath, string waveBankPath)
        {
            try
            {
                _path = soundBankPath.Substring(0, soundBankPath.Length - 4);
                _soundBank = new SoundBank(TorqueEngineComponent.Instance.SFXDevice, soundBankPath);
                _waveBank = new WaveBank(TorqueEngineComponent.Instance.SFXDevice, waveBankPath);
            }
            catch (Exception e)
            {
                TorqueConsole.Error("\nSoundGroup failed to initialize for path \"{0}\". \nThe following exception was thrown: \n{1}", soundBankPath, e.Message);
            }
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The SoundBank object associated with this SoundGroup.
        /// </summary>
        public SoundBank SoundBank
        {
            get { return _soundBank; }
        }



        /// <summary>
        /// The WaveBank object associated with this SoundGroup.
        /// </summary>
        public WaveBank WaveBank
        {
            get { return _waveBank; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Attempt to play the specified sound cue on this SoundGroup's SoundBank and return an accurate Cue for the sound.
        /// </summary>
        /// <param name="cueIndex">The name of the sound cue to play as it appears in your SoundBank (case sensitive).</param>
        /// <returns>The Cue object of the sound that was actually played. (I know this sounds obvious, but the normal sound
        /// methods don't do this).</returns>
        public Cue PlaySound(string cueIndex)
        {
            try
            {
                Cue newCue = GetCue(cueIndex);

                if (newCue != null)
                    newCue.Play();

                return newCue;
            }
            catch (InvalidOperationException) { }
            catch (IndexOutOfRangeException e)
            {
                TorqueConsole.Error("\nWarning: SoundGroup PlaySound failed for cue '{0}' in SoundBank '{1}' (possibly due to a missing or misspelled sound cue).\n{2}", cueIndex, _path, e.Message);
            }

            return null;
        }



        /// <summary>
        /// Attempt to play the specified sound cue on this SoundGroup's SoundBank at the specified distance and return an 
        /// accurate Cue for the sound.
        /// </summary>
        /// <param name="cueIndex">The name of the sound cue to play as it appears in your SoundBank (case sensitive).</param>
        /// <param name="distance">The distance to set the sound at. This assumes you set up your sounds to recieve a variable
        /// called 'Distance' and then set the volume accordingly via an RPC. See the XACT docs for details on how to do this.</param>
        /// <returns>The Cue object of the sound that was actually played. (I know this sounds obvious, but the normal XNA sound
        /// methods don't do this).</returns>
        public Cue PlaySound(string cueIndex, float distance)
        {
            if (distance > SoundManager.Instance.MaxSoundDistance)
                return null;

            try
            {
                Cue newCue = GetCue(cueIndex);

                if (newCue != null)
                {
                    newCue.SetVariable("Distance", distance);

                    if (newCue.IsPrepared)
                    {
                        newCue.Play();
                        return newCue;
                    }
                }
            }
            catch (IndexOutOfRangeException e)
            {
                TorqueConsole.Error("\nWarning: SoundGroup PlaySound failed for cue '{0}' in SoundBank '{1}' (possibly due to a missing or misspelled sound cue, or missing 'Distance' variable in sound bank).\n{2}", cueIndex, _path, e.Message);
            }
            catch (InvalidOperationException) { }
            catch (Microsoft.Xna.Framework.Audio.InstancePlayLimitException) { }

            return null;
        }



        /// <summary>
        /// Create and return a Cue object for the given sound cue name without playing it.
        /// </summary>
        /// <param name="cueIndex">The name of the sound cue to create a Cue object for.</param>
        /// <returns>A Cue object for the specified sound cue name.</returns>
        public Cue GetCue(string cueIndex)
        {
            try
            {
                return _soundBank.GetCue(cueIndex);
            }
            catch (IndexOutOfRangeException e)
            {
                TorqueConsole.Error("\nWarning: SoundGroup GetCue failed for cue '{0}' in SoundBank '{1}' (possibly due to a missing or misspelled sound cue).\n{2}", cueIndex, _path, e.Message);
            }
            catch (Microsoft.Xna.Framework.Audio.InstancePlayLimitException) { }

            return null;
        }

        #endregion


        #region Private, protected, internal fields

        /// <summary>
        /// The SoundBank object associated with this SoundGroup.
        /// </summary>
        protected SoundBank _soundBank;



        /// <summary>
        /// The WaveBank object associated with this SoundGroup.
        /// </summary>
        protected WaveBank _waveBank;



        /// <summary>
        /// The path that this SoundGroup is associated with.
        /// </summary>
        protected string _path;

        #endregion
    }



    /// <summary>
    /// A singleton class that's intended to simplify the use of sounds in XNA. It does so by providing a common place to access sounds, 
    /// a smoother interface, and some much-needed shortcuts. It allows you to play a sound and get that sound Cue object in one shot.
    /// It also allows you to register groups of sounds based on SoundBanks without the hassle of keeping track of SoundBank and WaveBank
    /// references. You don't have to worry about duplicate instances of SoundGroups because they're hashed. This also helps prevent the
    /// awful tendency of the garbage collector to stop sounds before they finish by garbage-collecting unreferenced sound cues.
    /// </summary>
    public class SoundManager
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// The static singleton instance of the SoundManager.
        /// </summary>
        static public SoundManager Instance
        {
            get
            {
                if (_soundManager == null)
                    _soundManager = new SoundManager();

                return _soundManager;
            }
        }



        /// <summary>
        /// A static boolean to check whether or not a SoundManager has been created. This exists to allow you to check for one without
        /// using the Instance property, which will automatically create one if none exists.
        /// </summary>
        static public bool IsCreated
        {
            get { return _soundManager != null; }
        }



        /// <summary>
        /// The static singleton instance of the SoundManager.
        /// </summary>
        static protected SoundManager _soundManager;

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The maximum distance at which to allow sounds to play.
        /// </summary>
        public float MaxSoundDistance
        {
            get { return _maxSoundDistance; }
            set { _maxSoundDistance = value; }
        }



        /// <summary>
        /// Specifies the number of stopped cues to remove from the end of the cues list when performing a cleanup pass. The default is 5.
        /// </summary>
        public int NumCuesCleanedOnCleanup
        {
            get { return _cleanupSize; }
            set { _cleanupSize = value; }
        }



        /// <summary>
        /// Specifies the amount of time in milliseconds to wait between cleanup passes. Default is one second
        /// </summary>
        public float CleanupFrequency
        {
            get { return _cleanupFrequency; }
            set { _cleanupFrequency = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Create and register a SoundGroup based on the specified SoundBank and WaveBank paths.
        /// </summary>
        /// <param name="name">The name to register the sound bank with.</param>
        /// <param name="soundBankPath">The path to the .xsb file.</param>
        /// <param name="waveBankPath">The path to the .xWb file.</param>
        public virtual SoundGroup RegisterSoundGroup(string name, string waveBankPath, string soundBankPath)
        {
            if (!_soundGroups.Contains(name))
            {
                SoundGroup newGroup = new SoundGroup(soundBankPath, waveBankPath);

                if (newGroup != null && newGroup.SoundBank != null && newGroup.WaveBank != null)
                    _soundGroups.Add(name, newGroup);

                return newGroup;
            }

            return null;
        }



        /// <summary>
        /// Removes the sound group that was registered with the specified SoundBank path from the hash.
        /// </summary>
        /// <param name="path">The path that the SoundGroup was registered under. Either the common path name in the case of the single-string
        /// register, or the path to the SoundBank (with the .xsb extention) in the case of the two-string register.</param>
        public virtual void UnregisterSoundGroup(SoundGroup group)
        {
            // clear and remove the specified sound group from the hash
            if (_soundGroups.ContainsValue(group))
            {
                if (!group.SoundBank.IsDisposed)
                    group.SoundBank.Dispose();

                if (!group.WaveBank.IsDisposed)
                    group.WaveBank.Dispose();

                _soundGroups.Remove(group);
            }
        }



        public virtual SoundGroup GetSoundGroup(string name)
        {
            if (_soundGroups.Contains(name))
                return _soundGroups[name] as SoundGroup;

            return null;
        }



        public virtual Cue PlaySound(string soundGroupName, string cueIndex)
        {
            SoundGroup soundGroup = GetSoundGroup(soundGroupName);

            if (soundGroup == null)
                return null;

            Cue newCue = soundGroup != null ? soundGroup.PlaySound(cueIndex) : null;

            if (newCue != null)
            {
                _cues.Add(newCue);
            }
            _cleanup(_cleanupSize);

            return newCue;
        }



        public virtual Cue PlaySound(string soundGroupName, string cueIndex, float distance)
        {
            SoundGroup soundGroup = GetSoundGroup(soundGroupName);

            if (soundGroup == null || distance > MaxSoundDistance)
                return null;

            Cue newCue = soundGroup != null ? soundGroup.PlaySound(cueIndex, distance) : null;

            if (newCue != null)
            {
                _cues.Add(newCue);
            }
            _cleanup(_cleanupSize);

            return newCue;
        }



        /// <summary>
        /// Acts exactly the same as PlaySound, except it doesn't play the sound for you.
        /// </summary>
        /// <param name="path">The SoundBank path that a SoundGroup was registered with using the two-string register, or the common path to a SoundBank and
        /// WaveBank without the extention.</param>
        /// <param name="cueIndex">The name of the sound cue to play as it appears in your SoundBank (case sensitive).</param>
        /// <returns>The Cue object of the specified sound.</returns>
        public virtual Cue GetCue(string soundGroupName, string cueIndex)
        {
            SoundGroup soundGroup = GetSoundGroup(soundGroupName);

            if (soundGroup == null)
                return null;

            return soundGroup.GetCue(cueIndex);
        }



        /// <summary>
        /// Traverses the entire cue list and removes all stopped sounds to allow the garbage collector to delete them. This is 
        /// normally done bit by bit as the SoundManager is used, but you may want to call this at the end of a level or whatever.
        /// </summary>
        public virtual void CleanAllCues()
        {
            // call cleanup with -1 specified for cuesToClean
            _cleanup(-1);
        }



        /// <summary>
        /// Immediately stops all sound queues.
        /// </summary>
        public virtual void StopAllCues()
        {
            // stop all cues immediately
            for (int i = 0; i < _cues.Count; i++)
                if (!_cues[i].IsDisposed)
                    _cues[i].Stop(AudioStopOptions.AsAuthored);

            // clean up all cues
            // (empty the cues list entirely)
            _cleanup(-1);
        }



        /// <summary>
        /// Reset the sound manager. Call this if you want to stop all sounds immediately, clear the cue list,
        /// and unregister all sound groups.
        /// </summary>
        public virtual void Reset()
        {
            // stop all cues immediately
            for (int i = 0; i < _cues.Count; i++)
                if (!_cues[i].IsDisposed)
                    _cues[i].Stop(AudioStopOptions.Immediate);

            // clean up all cues
            // (empty the cues list entirely)
            _cleanup(-1);

            // unregister all sound groups
            foreach (DictionaryEntry myDE in _soundGroups)
                UnregisterSoundGroup(myDE.Value as SoundGroup);
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Performs a cleanup pass on the cue list. Removes a number of stopped sounds so they may be garbage collected.
        /// The number removed per-cleanup is specified by NumCuesCleanedOnCleanup and is passed to this function when 
        /// PlaySound calls it.
        /// </summary>
        /// <param name="cuesToClean">The number of cues to clean on this pass. When used internally, this number will normally be 
        /// based on the NumCuesCleanedOnCleanup public property.</param>
        protected virtual void _cleanup(int cuesToClean)
        {
            float currTime = TorqueEngineComponent.Instance.TorqueTime;

            // early out if we have cleaned up within the time specified by _cleanupFrequency
            if (currTime - _lastCleanupTime < _cleanupFrequency)
                return;

            // record this as the last time a cleanup occurred
            _lastCleanupTime = currTime;

            // if -1 was specified for cuesToClean, clean all cues
            if (cuesToClean == -1)
                cuesToClean = _cues.Count;

            // iterate through the list and remove a set number of stopped cues
            for (int i = _cues.Count - 1; i >= 0 && i < _cues.Count && cuesToClean > 0; i--)
            {
                Cue sound = _cues[i];

                if (sound.IsStopped)
                {
                    _cues.Remove(sound);
                    sound.Dispose();
                    cuesToClean--;
                }
            }
        }

        #endregion


        #region Private, protected, internal fields

        /// <summary>
        /// Hash table that stores all the sound groups for this sound manager.
        /// </summary>
        protected Hashtable _soundGroups = new Hashtable();



        /// <summary>
        /// A table of all cues that have been played since last cleanup cycle.
        /// </summary>
        protected List<Cue> _cues = new List<Cue>();

        // cleanup fields
        private int _cleanupSize = 50;
        private float _cleanupFrequency = 1000;
        private float _lastCleanupTime;
        protected float _maxSoundDistance = 500;

        #endregion
    }
}
