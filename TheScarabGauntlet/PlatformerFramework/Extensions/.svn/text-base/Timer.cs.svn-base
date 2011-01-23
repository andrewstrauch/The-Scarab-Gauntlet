using System;
using System.Collections.Generic;
 using Microsoft.Xna.Framework;
using System.Text;

namespace GarageGames.Torque.PlatformerFramework
{
     ///<summary>
     /// Stopwatch timer with "TimeUntilStart" + "TimeToExpire" to allow timed start delay as well as timer duration
     ///</summary>
     public class StopwatchTimer
     {
         private readonly Timer _timer1 = new Timer();
         private readonly Timer _timer2 = new Timer();
  
         ///<summary>
         /// Constructor
         ///</summary>
         ///<param name="name"></param>
         public StopwatchTimer(String name)
         {
             Name = name;
  
             Reset();
         }
  
         ///<summary>
         /// Constructor
         ///</summary>
         public StopwatchTimer()
         {
             Name = "Unnamed";
             Reset();
         }
  
         ///<summary>
         /// Name of timer.
         ///</summary>
         public string Name { get; set; }
  
         ///<summary>
         /// Has timer expired?
         ///</summary>
         public bool Expired
         {
             get { return _timer2.Expired; }
         }
  
         ///<summary>
         /// Has timer started yet?
         ///</summary>
         public bool Started
         {
             get { return _timer1.Expired; }
         }
  
         ///<summary>
         /// Resets stopwatch timer.
         ///</summary>
         public void Reset()
         {
             _timer1.Reset();
             _timer2.Reset();
         }
  
         ///<summary>
         /// Update method, checks state of stopwatch and removes from manager if it has expired.
         ///</summary>
         ///<param name="time"></param>
         public void Update(GameTime time)
         {
             if (_timer1.Expired && !_timer2.Running && !_timer2.Expired)
             {
                 //   _timer1.Reset();
                 _timer2.Start();
             }
  
             if (_timer2.Expired)
             {
                 TimerManager.Instance.Remove(this);
             }
         }
  
         ///<summary>
         /// Delta between 0 - 1 signifying how far through timer we've gone.
         ///</summary>
         ///<returns></returns>
         public float Delta
         {
             get
             {
                 if (_timer2.Running)
                 {
                     return _timer2.Delta;
                 }
                 if (!Started)
                     return 0.0f;
                 if (Expired)
                     return 1.0f;
  
                 return 0.0f;
             }
         }
  
         ///<summary>
         /// Returns an interpolated floating point value
         ///</summary>
         ///<param name="start">Value when timer starts.</param>
         ///<param name="end">Value when timer ends.</param>
         ///<returns></returns>
         public float FloatSmoothStep(float start, float end)
         {
             float fDelta = Delta;
  
             return MathHelper.SmoothStep(start, end, fDelta);
         }
  
         ///<summary>
         /// Returns an interpolated Vector2 value
         ///</summary>
         ///<param name="start">Value when timer starts.</param>
         ///<param name="end">Value when timer ends.</param>
         ///<returns></returns>
         public Vector2 Vector2SmoothStep(Vector2 start, Vector2 end)
         {
             float fDelta = Delta;
  
             return Vector2.SmoothStep(start, end, fDelta);
         }
  
         ///<summary>
         /// Returns an interpolated Vector3 value
         ///</summary>
         ///<param name="start">Value when timer starts.</param>
         ///<param name="end">Value when timer ends.</param>
         ///<returns></returns>
         public Vector3 Vector3SmoothStep(Vector3 start, Vector3 end)
         {
             float fDelta = Delta;
  
             return Vector3.SmoothStep(start, end, fDelta);
         }
  
         ///<summary>
         /// Returns an interpolated Vector2 value
         ///</summary>
         ///<param name="start">Value when timer starts.</param>
         ///<param name="end">Value when timer ends.</param>
         ///<returns></returns>
         public Vector2 Vector2Lerp(Vector2 start, Vector2 end)
         {
             float fDelta = Delta;
  
             return Vector2.Lerp(start, end, fDelta);
         }
  
         ///<summary>
         /// Returns an interpolated Vector3 value
         ///</summary>
         ///<param name="start">Value when timer starts.</param>
         ///<param name="end">Value when timer ends.</param>
         ///<returns></returns>
         public Vector3 Vector3Lerp(Vector3 start, Vector3 end)
         {
             float fDelta = Delta;
  
             return Vector3.Lerp(start, end, fDelta);
         }
  
         ///<summary>
         /// Returns an interpolated Vector4 value
         ///</summary>
         ///<param name="start">Value when timer starts.</param>
         ///<param name="end">Value when timer ends.</param>
         ///<returns></returns>
         public Vector4 Vector4Lerp(Vector4 start, Vector4 end)
         {
             float fDelta = Delta;
  
             return Vector4.Lerp(start, end, fDelta);
         }
  
         ///<summary>
         /// Amount of time before timer expires
         ///</summary>
         ///<param name="value"></param>
         public float SecondsUntilExpire
         {
             set { _timer2.SecondsUntilExpire = value; }
             get { return _timer2.SecondsUntilExpire; }
         }
  
         ///<summary>
         ///  Set amount of time before timer starts
         ///</summary>
         ///<returns></returns>
         public float SecondsUntilStart
         {
             get { return _timer1.SecondsUntilExpire; }
             set { _timer1.SecondsUntilExpire = value; }
         }
  
         ///<summary>
         /// Amount of time before timer expires
         ///</summary>
         ///<param name="value"></param>
         public float MillisecondsUntilExpire
         {
             set { _timer2.MillisecondsUntilExpire = value; }
             get { return _timer2.MillisecondsUntilExpire; }
         }
  
         ///<summary>
         ///  Set amount of time before timer starts
         ///</summary>
         ///<returns></returns>
         public float MillisecondsUntilStart
         {
             get { return _timer1.MillisecondsUntilExpire; }
             set { _timer1.MillisecondsUntilExpire = value; }
         }
  
         ///<summary>
         /// Starts the timer and adds it to the timer manager.
         ///</summary>
         public void Start()
         {
             _timer2.Reset();
             _timer1.Start();
             if (!TimerManager.Instance.StopwatchTimerList.Contains(this))
                 TimerManager.Instance.Add(this);
         }
     }
  
  
     ///<summary>
     /// Timer class, for simple timing in seconds / milliseconds
     ///</summary>
     public class Timer
     {
  
         ///<summary>
         /// Constructor
         ///</summary>
         ///<param name="name"></param>
         public Timer(String name)
         {
             Name = name;
  
             Reset();
         }
  
         ///<summary>
         /// Constructor
         ///</summary>
         public Timer()
         {
             Name = "Unnamed";
             Reset();
         }
  
         ///<summary>
         /// Name of timer.
         ///</summary>
         public string Name { get; set; }
  
         ///<summary>
         /// Is timer running?
         ///</summary>
         public bool Running { get; set; }
  
         ///<summary>
         /// Has the timer expired?
         ///</summary>
         public bool Expired { get; private set; }
  
         ///<summary>
         /// Destructor. Removes timer from <see cref="TimerManager"/>
         ///</summary>
         ~Timer()
         {
             TimerManager.Instance.TimerList.Remove(this);
         }
  
         ///<summary>
         /// Resets the timer.
         ///</summary>
         public void Reset()
         {
             Expired = false;
             Running = false;
             _timeExpired = 0;
         }
  
         ///<summary>
         /// Returns delta between 0 and 1 signifying how far through time we are.
         ///</summary>
         ///<returns></returns>
         public float Delta
         {
             get
             {
                 if (_timeTarget != -1)
                 {
                     return _timeExpired/_timeTarget;
                 }
  
                 return 0.0f;
             }
         }
  
         ///<summary>
         /// Returns an interpolated float value.
         ///</summary>
         ///<param name="start">Value when timer starts.</param>
         ///<param name="end">Value when timer ends.</param>
         ///<returns></returns>
         public float FloatSmoothStep(float start, float end)
         {
             float delta = Delta;
  
             return MathHelper.SmoothStep(start, end, delta);
         }
  
         ///<summary>
         /// Returns an interpolated Vector2 value.
         ///</summary>
         ///<param name="start">Value when timer starts.</param>
         ///<param name="end">Value when timer ends.</param>
         ///<returns></returns>
         public Vector2 Vector2SmoothStep(Vector2 start, Vector2 end)
         {
             float fDelta = Delta;
  
             return Vector2.SmoothStep(start, end, fDelta);
         }
  
         ///<summary>
         /// Returns an interpolated Vector2 value.
         ///</summary>
         ///<param name="start">Value when timer starts.</param>
         ///<param name="end">Value when timer ends.</param>
         ///<returns></returns>
         public Vector2 Vector2Lerp(Vector2 start, Vector2 end)
         {
             var fDelta = Delta;
  
             return Vector2.Lerp(start, end, fDelta);
         }
  
         ///<summary>
         /// Returns an interpolated Vector3 value.
         ///</summary>
         ///<param name="start">Value when timer starts.</param>
         ///<param name="end">Value when timer ends.</param>
         ///<returns></returns>
         public Vector3 Vector3Lerp(Vector3 start, Vector3 end)
         {
             var fDelta = Delta;
  
             return Vector3.Lerp(start, end, fDelta);
         }
  
         ///<summary>
         /// Returns an interpolated Vector4 value.
         ///</summary>
         ///<param name="start">Value when timer starts.</param>
         ///<param name="end">Value when timer ends.</param>
         ///<returns></returns>
         public Vector4 Vector4Lerp(Vector4 start, Vector4 end)
         {
             var fDelta = Delta;
             var vec = Vector4.Lerp(start, end, fDelta);
             return vec;
         }
  
  
         ///<summary>
         /// Starts the timer.
         ///</summary>
         public void Start()
         {
             if (!Running && !TimerManager.Instance.TimerList.Contains(this))
                 TimerManager.Instance.Add(this);
  
             Expired = false;
             _timeExpired = 0;
             Running = true;
         }
  
         ///<summary>
         /// Stops the timer.
         ///</summary>
         public void Stop()
         {
             Running = false;
             TimerManager.Instance.Remove(this);
         }
  
         ///<summary>
         /// Updates the timer, expires it if time has run out.
         ///</summary>
         ///<param name="time"></param>
         public void Update(GameTime time)
         {
             if (!Running) return;
             _timeExpired += time.ElapsedGameTime.Milliseconds;
  
             if (_timeExpired >= _timeTarget && _timeTarget != -1)
             {
                 Expired = true;
                 Stop();
             }
         }
  
         ///<summary>
         /// Seconds until timer expires.
         ///</summary>
         ///<param name="value"></param>
         public float SecondsUntilExpire
         {
             set { _timeTarget = (int)(value * 1000); }
             get { return _timeTarget / 1000.0f; }
         }
  
         ///<summary>
         /// Milliseconds until timer expires.
         ///</summary>
         ///<returns></returns>
         public float MillisecondsUntilExpire
         {
             set { _timeTarget = (int)(value); }
             get { return _timeTarget; }
        }
  
         private float _timeExpired;
         private float _timeTarget;
  
     }
  
     ///<summary>
     /// Main TimerManager singleton class. Call Instance.Update from main update.
     ///</summary>
     public sealed class TimerManager
     {
         ///<summary>
         /// List of Stopwatch Timers currently running.
         ///</summary>
         internal List<StopwatchTimer> StopwatchTimerList
         {
             get { return _stopwatchTimerList; }
         }
  
         ///<summary>
         /// List of standard timers currently running.
         ///</summary>
         internal List<Timer> TimerList
         {
             get { return _timerList; }
         }
  
         ///<summary>
         /// Remove a stopwatch timer.
         ///</summary>
         ///<param name="timer"></param>
         public void Remove(StopwatchTimer timer)
         {
             _stopwatchRemoveList.Add(timer);
         }
  
         ///<summary>
         /// add a stopwatch timer.
         ///</summary>
         ///<param name="timer"></param>
         public void Add(StopwatchTimer timer)
         {
             _stopwatchTimerList.Add(timer);
         }
  
         ///<summary>
         /// Remove a timer.
         ///</summary>
         ///<param name="timer"></param>
         public void Remove(Timer timer)
         {
             _removeList.Add(timer);
         }
  
         ///<summary>
         /// Add a timer.
         ///</summary>
         ///<param name="timer"></param>
         public void Add(Timer timer)
         {
             _timerList.Add(timer);
         }
  
         ///<summary>
         /// Main update method.
         ///</summary>
         ///<param name="time"></param>
         public void Update(GameTime time)
         {
             foreach (var timer in _timerList)
             {
                 timer.Update(time);
             }
             foreach (var timer in _stopwatchTimerList)
             {
                 timer.Update(time);
             }
  
             foreach (var timer in _removeList)
             {
                 _timerList.Remove(timer);
             }
             foreach (var timer in _stopwatchRemoveList)
             {
                 _stopwatchTimerList.Remove(timer);
             }
  
             _removeList.Clear();
             _stopwatchRemoveList.Clear();
         }
  
         private readonly List<Timer> _removeList = new List<Timer>();
         private readonly List<StopwatchTimer> _stopwatchRemoveList = new List<StopwatchTimer>();
         private readonly List<StopwatchTimer> _stopwatchTimerList = new List<StopwatchTimer>();
         private readonly List<Timer> _timerList = new List<Timer>();
  
         private static readonly TimerManager _instance = new TimerManager();
  
         ///<summary>
         /// Singleton access property.
         ///</summary>
         public static TimerManager Instance { get { return _instance; } }
  
      
     }
  
 }
