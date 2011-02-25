//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.TS
{
    public partial class ShapeInstance
    {
        #region Public methods

        /// <summary>
        /// Animates the shape instance at the current detail level.
        /// </summary>
        public void Animate()
        {
            Animate(_currentDetailLevel);
        }



        /// <summary>
        /// Animates the shape instance at the specified detail level.
        /// </summary>
        /// <param name="dl">The detail level to animate.</param>
        public void Animate(int dl)
        {
            if (dl == -1)
                return; // nothing to do

            int ss = _shape.Details[dl].SubShapeNumber;

            if (ss < 0)
                return; // this is a billboard detail...

            DirtyFlags dirtyFlags = _dirtyFlags[ss];

            if ((dirtyFlags & DirtyFlags.ThreadDirty) != DirtyFlags.NoneDirty && _threadList != null)
            {
                _threadList.Sort();
                _transitionThreads.Sort();
            }

            // Animate ifl's?
            if ((dirtyFlags & DirtyFlags.IflDirty) != DirtyFlags.NoneDirty)
                _AnimateIfls();

            // Animate nodes?
            if ((dirtyFlags & DirtyFlags.TransformDirty) != DirtyFlags.NoneDirty)
                _AnimateNodes(ss);

            // Animate objects?
            if ((dirtyFlags & DirtyFlags.VisDirty) != DirtyFlags.NoneDirty)
                _AnimateVisibility(ss);

            if ((dirtyFlags & DirtyFlags.FrameDirty) != DirtyFlags.NoneDirty)
                _AnimateFrame(ss);

            if ((dirtyFlags & DirtyFlags.MatFrameDirty) != DirtyFlags.NoneDirty)
                _AnimateMatFrame(ss);

            _dirtyFlags[ss] = DirtyFlags.NoneDirty;
        }



        /// <summary>
        /// Whether or not the shape has animated scale.
        /// </summary>
        /// <returns>True if scale animates.</returns>
        public bool AnimatesScale()
        {
            return (_shape.Flags & SequenceFlags.AnyScale) != 0;
        }



        /// <summary>
        /// Whether or not the shape has animated uniform scale.
        /// </summary>
        /// <returns>True if uniform scale animates.</returns>
        public bool AnimatesUniformScale()
        {
            return (_shape.Flags & SequenceFlags.UniformScale) != 0;
        }



        /// <summary>
        /// Whether or not the shape has animated aligned scale.
        /// </summary>
        /// <returns>True if aligned scale animates.</returns>
        public bool AnimatesAlignedScale()
        {
            return (_shape.Flags & SequenceFlags.AlignedScale) != 0;
        }



        /// <summary>
        /// Whether or not the shape has animated arbitrary scale.
        /// </summary>
        /// <returns>True if arbitrary scale animates.</returns>
        public bool AnimatesArbitraryScale()
        {
            return (_shape.Flags & SequenceFlags.ArbitraryScale) != 0;
        }



        /// <summary>
        /// Whether or not scale is currently being animated.
        /// </summary>
        /// <returns>True if scale is currently animated.</returns>
        public bool ScaleCurrentlyAnimated()
        {
            return _scaleCurrentlyAnimated;
        }



        /// <summary>
        /// Whether or not the shape is currently in a transition sequence.
        /// </summary>
        /// <returns>True if the shape is transitioning.</returns>
        public bool InTransition()
        {
            return _transitionThreads != null && _transitionThreads.Count != 0;
        }



        /// <summary>
        /// Gets the number of threads loaded on the shape instance.
        /// </summary>
        /// <returns>The number of threads.</returns>
        public int ThreadCount()
        {
            return _threadList.Count;
        }



        /// <summary>
        /// Adds a thread to the shape instance.
        /// </summary>
        /// <returns>The new thread.</returns>
        public Thread AddThread()
        {
            if (_shape.Sequences.Length == 0)
                return null;

            Thread thread = new Thread(this);
            _threadList.Add(thread);
            _SetDirty(DirtyFlags.AllDirtyMask);
            return thread;
        }



        /// <summary>
        /// Removes a thread from the shape instance.
        /// </summary>
        /// <param name="thread">The thread to remove.</param>
        public void DestroyThread(Thread thread)
        {
            if (thread == null)
                return;

            ClearTransition(thread);
            _threadList.Remove(thread);
            _SetDirty(DirtyFlags.AllDirtyMask);
            _CheckScaleCurrentlyAnimated();
        }



        /// <summary>
        /// Gets the sequence index the specified thread is loaded for.
        /// </summary>
        /// <param name="thread">The thread to look up.</param>
        /// <returns>The sequence index.</returns>
        public int GetSequence(Thread thread)
        {
            for (int i = 0; i < _shape.Sequences.Length; i++)
            {
                if (ReferenceEquals(thread.Sequence, _shape.Sequences[i]))
                    return i;
            }

            return -1;
        }



        /// <summary>
        /// Sets a sequence as the currently playing sequence.
        /// </summary>
        /// <param name="thread">The thread to play the sequence on.</param>
        /// <param name="seq">The index of the sequence to play.</param>
        /// <param name="pos">The position to start the sequence at.</param>
        public void SetSequence(Thread thread, int seq, float pos)
        {
            SetSequence(thread, _shape.Sequences[seq], pos);
        }



        /// <summary>
        /// Sets a sequence as the currently playing sequence.
        /// </summary>
        /// <param name="thread">The thread to play the sequence on.</param>
        /// <param name="seq">The sequence to play.</param>
        /// <param name="pos">The position to start the sequence at.</param>
        public void SetSequence(Thread thread, Sequence seq, float pos)
        {
            if ((thread._transitionData._inTransition && _transitionThreads.Count > 1) || _transitionThreads.Count > 0)
            {
                // if we have transitions, make sure transforms are up to date...
                _AnimateNodeSubtrees(true);
            }

            thread._SetSequence(seq, pos);
            _SetDirty(DirtyFlags.AllDirtyMask);

            if (_scaleCurrentlyAnimated && !thread.Sequence.IsScaleAnimated())
                _CheckScaleCurrentlyAnimated();
            else if (!_scaleCurrentlyAnimated && thread.Sequence.IsScaleAnimated())
                _scaleCurrentlyAnimated = true;

            _UpdateTransitions();
        }



        /// <summary>
        /// Transitions from the current sequence to a new sequence.
        /// </summary>
        /// <param name="thread">The thread to transition on.</param>
        /// <param name="seq">The index of the sequence to transition to.</param>
        /// <param name="pos">The position of the sequence to start at.</param>
        /// <param name="duration">The length of the transition.</param>
        /// <param name="continuePlay">Whether or not to continue playing the thread.</param>
        public void TransitionToSequence(Thread thread, int seq, float pos, float duration, bool continuePlay)
        {
            TransitionToSequence(thread, _shape.Sequences[seq], pos, duration, continuePlay);
        }



        /// <summary>
        /// Transitions from the current sequence to a new sequence.
        /// </summary>
        /// <param name="thread">The thread to transition on.</param>
        /// <param name="seq">The sequence to transition to.</param>
        /// <param name="pos">The position of the sequence to start at.</param>
        /// <param name="duration">The length of the transition.</param>
        /// <param name="continuePlay">Whether or not to continue playing the thread.</param>
        public void TransitionToSequence(Thread thread, Sequence seq, float pos, float duration, bool continuePlay)
        {
            // make sure all transforms on all detail levels are accurate
            _AnimateNodeSubtrees(true);

            thread.TransitionToSequence(seq, pos, duration, continuePlay);
            _SetDirty(DirtyFlags.AllDirtyMask);
            // cafTODO: don't use this, remove it altogether?
            //_groundThread = null;

            if (_scaleCurrentlyAnimated && !thread.Sequence.IsScaleAnimated())
                _CheckScaleCurrentlyAnimated();
            else if (!_scaleCurrentlyAnimated && thread.Sequence.IsScaleAnimated())
                _scaleCurrentlyAnimated = true;

            _transitionRotationNodes.Overlap(thread._transitionData._oldRotationNodes);
            _transitionRotationNodes.Overlap(thread.Sequence.DoesRotationMatter);

            _transitionTranslationNodes.Overlap(thread._transitionData._oldTranslationNodes);
            _transitionTranslationNodes.Overlap(thread.Sequence.DoesTranslationMatter);

            _transitionScaleNodes.Overlap(thread._transitionData._oldScaleNodes);
            _transitionScaleNodes.Overlap(thread.Sequence.DoesScaleMatter);

            // if we aren't already in the list of transition threads, add us now
            int i;
            for (i = 0; i < _transitionThreads.Count; i++)
            {
                if (ReferenceEquals(_transitionThreads[i], thread))
                    break;
            }

            if (i == _transitionThreads.Count)
                _transitionThreads.Add(thread);

            _UpdateTransitions();
        }



        /// <summary>
        /// Stop a transition on a thread.
        /// </summary>
        /// <param name="thread">The thread to stop the transition on.</param>
        public void ClearTransition(Thread thread)
        {
            if (!thread._transitionData._inTransition)
                return;

            // if other transitions are still playing,
            // make sure transforms are up to date
            if (_transitionThreads.Count > 1)
                _AnimateNodeSubtrees(true);

            // turn off transition...
            thread._transitionData._inTransition = false;

            // remove us from transition list
            if (_transitionThreads.Count != 0)
            {
                for (int i = 0; i < _transitionThreads.Count; i++)
                {
                    if (_transitionThreads[i] == thread)
                    {
                        _transitionThreads.RemoveRange(i, 1);
                        break;
                    }
                }
            }

            // recompute transitionNodes
            _transitionRotationNodes.ClearAll();
            _transitionTranslationNodes.ClearAll();
            _transitionScaleNodes.ClearAll();
            for (int i = 0; i < _transitionThreads.Count; i++)
            {
                _transitionRotationNodes.Overlap(_transitionThreads[i]._transitionData._oldRotationNodes);
                _transitionRotationNodes.Overlap(_transitionThreads[i].Sequence.DoesRotationMatter);

                _transitionTranslationNodes.Overlap(_transitionThreads[i]._transitionData._oldTranslationNodes);
                _transitionTranslationNodes.Overlap(_transitionThreads[i].Sequence.DoesTranslationMatter);

                _transitionScaleNodes.Overlap(_transitionThreads[i]._transitionData._oldScaleNodes);
                _transitionScaleNodes.Overlap(_transitionThreads[i].Sequence.DoesScaleMatter);
            }

            _SetDirty(DirtyFlags.ThreadDirty);

            _UpdateTransitions();
        }



        /// <summary>
        /// Gets whether or not a trigger is currently active.
        /// </summary>
        /// <param name="stateNum">The trigger index.</param>
        /// <param name="clearState">Whether or not to clear the state if it is set.</param>
        /// <returns>True if a trigger is active.</returns>
        public bool GetTriggerState(int stateNum, bool clearState)
        {
            Assert.Fatal(stateNum <= 32 && stateNum > 0, "ShapeInstance.GetTriggerState - State index out of range.");

            stateNum--; // stateNum externally 1..32, internally 0..31
            int bit = 1 << stateNum;
            bool ret = ((_triggerStates & bit) != 0);
            if (clearState)
                _triggerStates &= ~bit;

            return ret;
        }



        /// <summary>
        /// Add a Transform3D reference to a node.
        /// </summary>
        /// <param name="nodeIndex">The node index to ignore.</param>
        public void SetHandsOff(int nodeIndex)
        {
            SetHandsOff(nodeIndex, null);
        }



        /// <summary>
        /// Add a Transform3D reference to a node.
        /// </summary>
        /// <param name="nodeIndex">The node index to ignore.</param>
        /// <param name="transform">The transform to use.</param>
        public void SetHandsOff(int nodeIndex, Transform3D transform)
        {
            if (_handsOffNodes.Test(nodeIndex))
                return;

            _handsOffNodes.Set(nodeIndex);
            _disableBlendNodes.Set(nodeIndex);
            _SetDirty(DirtyFlags.AllDirtyMask);

            if (transform != null)
            {
                if (_callbackNodes == null)
                    _callbackNodes = new List<TSCallback>();

                _callbackNodes.Add(new TSCallback(nodeIndex, transform));
            }
        }



        /// <summary>
        /// Clear a Transform3D reference to a node.
        /// </summary>
        /// <param name="nodeIndex">The node index to clear.</param>
        public void ClearHandsOff(int nodeIndex)
        {
            if (!_handsOffNodes.Test(nodeIndex))
                return;

            _handsOffNodes.Clear(nodeIndex);
            _disableBlendNodes.Clear(nodeIndex);
            _SetDirty(DirtyFlags.AllDirtyMask);
            if (_callbackNodes != null)
            {
                for (int i = 0; i < _callbackNodes.Count; i++)
                {
                    if (_callbackNodes[i].NodeIndex == nodeIndex)
                    {
                        _callbackNodes.RemoveAt(i);
                        break;
                    }
                }
            }
        }



        /// <summary>
        /// Whether or not a node is referenced by a Transform3D.
        /// </summary>
        /// <param name="nodeIndex">The node to check.</param>
        /// <returns>True if the node is not in use.</returns>
        public bool IsHandsOff(int nodeIndex)
        {
            return _handsOffNodes.Test(nodeIndex);
        }

        #endregion


        #region Private, protected, internal methods

        void _AnimateNodeSubtrees(bool forceFull)
        {
            // Animate all the nodes for all the detail levels...

            if (forceFull)
                // force transforms to Animate
                _SetDirty(DirtyFlags.TransformDirty);

            for (int i = 0; i < _shape.SubShapeNodeCount.Length; i++)
            {
                if ((_dirtyFlags[i] & DirtyFlags.TransformDirty) != DirtyFlags.NoneDirty)
                    _AnimateNodes(i);
            }
        }



        void _AnimateSubtrees(bool forceFull)
        {
            // Animate all the subtrees

            if (forceFull)
                // force full Animate
                _SetDirty(DirtyFlags.AllDirtyMask);

            for (int i = 0; i < _shape.Details.Length; i++)
            {
                int ss = _shape.Details[i].SubShapeNumber;
                if (ss < 0)
                    continue; // skip billboards

                Animate(i);
            }
        }



        void _AnimateIfls()
        {
            // for each ifl material decide which thread controls it and Set it up
            for (int i = 0; i < _iflMaterialInstances.Length; i++)
            {
                _iflMaterialInstances[i].Frame = 0; // make sure that at least default value is Set
                for (int j = 0; j < _threadList.Count; j++)
                {
                    if (_threadList[j].Sequence.DoesIflMatter.Test(i))
                    {
                        // lookup ifl properties
                        int firstFrameOffTimeIndex = _iflMaterialInstances[i].IflMaterial.FirstFrameOffTimeIndex;
                        int numFrames = _iflMaterialInstances[i].IflMaterial.FrameCount;
                        float iflDur = numFrames != 0 ? _shape.IflFrameOffTimes[firstFrameOffTimeIndex + numFrames - 1] : 0.0f;

                        // where are we in the ifl
                        float time = _threadList[j]._pos * _threadList[j].Sequence.Duration + _threadList[j].Sequence.ToolBegin;

                        if (time > iflDur && iflDur > 0.0f)
                            // handle looping ifl
                            time -= iflDur * (float)((int)(time / iflDur));

                        // look up frame -- consider binary search
                        for (int k = 0; k < numFrames - 1 && time > _shape.IflFrameOffTimes[firstFrameOffTimeIndex + k]; k++)
                            _iflMaterialInstances[i].Frame = k;

                        break;
                    }
                }
            }

            // ifl is same for all sub-shapes, so Clear them all out now
            _ClearDirty(DirtyFlags.IflDirty);
        }



        void _AnimateVisibility(int ss)
        {
            if (_meshObjects.Length == 0)
                return;

            // find out who needs default values Set
            BitVector beenSet = new BitVector();
            beenSet.SetSize(_meshObjects.Length);
            beenSet.SetAll();
            for (int i = 0; i < _threadList.Count; i++)
                beenSet.TakeAway(_threadList[i].Sequence.DoesVisibilityMatter);

            // Set defaults
            int a = _shape.SubShapeFirstObject[ss];
            int b = a + _shape.SubShapeObjectCount[ss];
            for (int i = a; i < b; i++)
            {
                if (beenSet.Test(i))
                    _meshObjects[i].Visibility = _shape.ObjectStates[i].Visibility;
            }

            // go through each thread and Set visibility on those objects that
            // are not Set yet and are controlled by that thread
            BitVector objectMatters = new BitVector();
            for (int i = 0; i < _threadList.Count; i++)
            {
                Thread th = _threadList[i];

                objectMatters.Copy(ref th.Sequence.DoesFrameMatter);
                objectMatters.Overlap(th.Sequence.DoesMaterialFrameMatter);
                objectMatters.Overlap(th.Sequence.DoesVisibilityMatter);

                // skip to beginining of this sub-shape
                int j = 0;
                int start = objectMatters.Start();
                int end = b;
                for (int objectIndex = start; objectIndex < b; objectMatters.Next(ref objectIndex), j++)
                {
                    if (!beenSet.Test(objectIndex) && th.Sequence.DoesVisibilityMatter.Test(objectIndex))
                    {
                        float state1 = _shape.GetObjectState(th.Sequence, th._keyNum1, j).Visibility;
                        float state2 = _shape.GetObjectState(th.Sequence, th._keyNum2, j).Visibility;
                        if ((state1 - state2) * (state1 - state2) > 0.99f)
                            // goes from 0 to 1 -- discreet jump
                            _meshObjects[objectIndex].Visibility = th._keyPos < 0.5f ? state1 : state2;
                        else
                            // Interpolate between keyframes when visibility change is gradual
                            _meshObjects[objectIndex].Visibility = (1.0f - th._keyPos) * state1 + th._keyPos * state2;

                        // record change so that later threads don't over-write us...
                        beenSet.Set(objectIndex);
                    }
                }
            }
        }



        void _AnimateFrame(int ss)
        {
            if (_meshObjects.Length == 0)
                return;

            // find out who needs default values Set
            BitVector beenSet = new BitVector();
            beenSet.SetSize(_meshObjects.Length);
            beenSet.SetAll();
            for (int i = 0; i < _threadList.Count; i++)
                beenSet.TakeAway(_threadList[i].Sequence.DoesFrameMatter);

            // Set defaults
            int a = _shape.SubShapeFirstObject[ss];
            int b = a + _shape.SubShapeObjectCount[ss];
            for (int i = a; i < b; i++)
                if (beenSet.Test(i))
                    _meshObjects[i].Frame = _shape.ObjectStates[i].FrameIndex;

            // go through each thread and Set frame on those objects that
            // are not Set yet and are controlled by that thread
            BitVector objectMatters = new BitVector();
            for (int i = 0; i < _threadList.Count; i++)
            {
                Thread th = _threadList[i];

                objectMatters.Copy(ref th.Sequence.DoesFrameMatter);
                objectMatters.Overlap(th.Sequence.DoesMaterialFrameMatter);
                objectMatters.Overlap(th.Sequence.DoesVisibilityMatter);

                // skip to beginining of this sub-shape
                int j = 0;
                int start = objectMatters.Start();
                int end = b;
                for (int objectIndex = start; objectIndex < b; objectMatters.Next(ref objectIndex), j++)
                {
                    if (!beenSet.Test(objectIndex) && th.Sequence.DoesFrameMatter.Test(objectIndex))
                    {
                        int key = (th._keyPos < 0.5f) ? th._keyNum1 : th._keyNum2;
                        _meshObjects[objectIndex].Frame = _shape.GetObjectState(th.Sequence, key, j).FrameIndex;

                        // record change so that later threads don't over-write us...
                        beenSet.Set(objectIndex);
                    }
                }
            }
        }



        void _AnimateMatFrame(int ss)
        {
            if (_meshObjects.Length == 0)
                return;

            // find out who needs default values Set
            BitVector beenSet = new BitVector();
            beenSet.SetSize(_meshObjects.Length);
            beenSet.SetAll();
            for (int i = 0; i < _threadList.Count; i++)
                beenSet.TakeAway(_threadList[i].Sequence.DoesMaterialFrameMatter);

            // Set defaults
            int a = _shape.SubShapeFirstObject[ss];
            int b = a + _shape.SubShapeObjectCount[ss];
            for (int i = a; i < b; i++)
            {
                if (beenSet.Test(i))
                    _meshObjects[i].MaterialFrame = _shape.ObjectStates[i].MaterialFrameIndex;
            }

            // go through each thread and Set matFrame on those objects that
            // are not Set yet and are controlled by that thread
            BitVector objectMatters = new BitVector();
            for (int i = 0; i < _threadList.Count; i++)
            {
                Thread th = _threadList[i];

                objectMatters.Copy(ref th.Sequence.DoesFrameMatter);
                objectMatters.Overlap(th.Sequence.DoesMaterialFrameMatter);
                objectMatters.Overlap(th.Sequence.DoesVisibilityMatter);

                // skip to beginining of this sub-shape
                int j = 0;
                int start = objectMatters.Start();
                int end = b;
                for (int objectIndex = start; objectIndex < end; objectMatters.Next(ref objectIndex), j++)
                {
                    if (!beenSet.Test(objectIndex) && th.Sequence.DoesMaterialFrameMatter.Test(objectIndex))
                    {
                        int key = (th._keyPos < 0.5f) ? th._keyNum1 : th._keyNum2;
                        _meshObjects[objectIndex].MaterialFrame = _shape.GetObjectState(th.Sequence, key, j).MaterialFrameIndex;

                        // record change so that later threads don't over-write us...
                        beenSet.Set(objectIndex);
                    }
                }
            }
        }



        void _AnimateNodes(int ss)
        {
            if (_shape.Nodes.Length == 0)
                return;

            // temporary storage for node transforms
            int numNodes = _shape.Nodes.Length;
            if (_nodeCurrentRotations == null || _nodeCurrentRotations.Length < numNodes)
            {
                // grow all these arrays together...no need to check each individually
                TorqueUtil.GrowArray<Quaternion>(ref _nodeCurrentRotations, numNodes);
                TorqueUtil.GrowArray<Vector3>(ref _nodeCurrentTranslations, numNodes);
                TorqueUtil.GrowArray<Thread>(ref _workRotationThreads, numNodes);
                TorqueUtil.GrowArray<Thread>(ref _workTranslationThreads, numNodes);
            }

            BitVector rotBeenSet = new BitVector();
            BitVector tranBeenSet = new BitVector();
            BitVector scaleBeenSet = new BitVector();
            rotBeenSet.SetSize(numNodes);
            rotBeenSet.SetAll();
            tranBeenSet.SetSize(numNodes);
            tranBeenSet.SetAll();
            scaleBeenSet.SetSize(numNodes);
            scaleBeenSet.SetAll();

            int firstBlend = _threadList.Count;
            for (int i = 0; i < _threadList.Count; i++)
            {
                Thread th = _threadList[i];

                if (th.Sequence.IsBlend())
                {
                    // blend sequences need default (if not Set by other _sequence)
                    // break rather than continue because the rest will be blends too
                    firstBlend = i;
                    break;
                }
                rotBeenSet.TakeAway(th.Sequence.DoesRotationMatter);
                tranBeenSet.TakeAway(th.Sequence.DoesTranslationMatter);
                scaleBeenSet.TakeAway(th.Sequence.DoesScaleMatter);
            }

            rotBeenSet.TakeAway(_handsOffNodes);
            tranBeenSet.TakeAway(_handsOffNodes);

            // all the nodes marked above need to have the default transform
            int a = _shape.SubShapeFirstNode[ss];
            int b = a + _shape.SubShapeNodeCount[ss];
            for (int i = a; i < b; i++)
            {
                if (rotBeenSet.Test(i))
                {
                    _shape.DefaultRotations[i].Get(out _nodeCurrentRotations[i]);
                    _workRotationThreads[i] = null;
                }

                if (tranBeenSet.Test(i))
                {
                    _nodeCurrentTranslations[i] = _shape.DefaultTranslations[i];
                    _workTranslationThreads[i] = null;
                }
            }

            // don't want a transform in these cases...
            rotBeenSet.Overlap(_handsOffNodes);
            tranBeenSet.Overlap(_handsOffNodes);

            // default Scale
            if (ScaleCurrentlyAnimated())
                _HandleDefaultScale(a, b, ref scaleBeenSet);

            // handle non-blend sequences
            for (int i = 0; i < firstBlend; i++)
            {
                Thread th = _threadList[i];

                int nodeIndex = th.Sequence.DoesRotationMatter.Start();
                int end = b;
                for (int j = 0; nodeIndex < end; th.Sequence.DoesRotationMatter.Next(ref nodeIndex), j++)
                {
                    // skip nodes outside of this detail
                    if (nodeIndex < a)
                        continue;

                    if (!rotBeenSet.Test(nodeIndex))
                    {
                        Quaternion q1, q2;
                        _shape.GetRotation(th.Sequence, th._keyNum1, j, out q1);
                        _shape.GetRotation(th.Sequence, th._keyNum2, j, out q2);
                        Transform.Interpolate(q1, q2, th._keyPos, out _nodeCurrentRotations[nodeIndex]);
                        rotBeenSet.Set(nodeIndex);
                        _workRotationThreads[nodeIndex] = th;
                    }
                }

                nodeIndex = th.Sequence.DoesTranslationMatter.Start();
                end = b;
                for (int j = 0; nodeIndex < end; th.Sequence.DoesTranslationMatter.Next(ref nodeIndex), j++)
                {
                    if (nodeIndex < a)
                        continue;
                    if (!tranBeenSet.Test(nodeIndex))
                    {
                        Vector3 p1 = _shape.GetTranslation(th.Sequence, th._keyNum1, j);
                        Vector3 p2 = _shape.GetTranslation(th.Sequence, th._keyNum2, j);
                        Transform.Interpolate(p1, p2, th._keyPos, out _nodeCurrentTranslations[nodeIndex]);
                        _workTranslationThreads[nodeIndex] = th;
                        tranBeenSet.Set(nodeIndex);
                    }
                }

                if (ScaleCurrentlyAnimated())
                    _HandleAnimatedScale(th, a, b, ref scaleBeenSet);
            }

            // transitions...
            if (InTransition())
                _HandleTransitionNodes(a, b);

            // compute transforms
            for (int i = a; i < b; i++)
                if (!_handsOffNodes.Test(i))
                    Transform.SetMatrix(_nodeCurrentRotations[i], _nodeCurrentTranslations[i], out _nodeTransforms[i]);

            // add Scale onto transforms
            if (ScaleCurrentlyAnimated())
                _HandleNodeScale(a, b);

            // get callback transforms...
            if (_callbackNodes != null)
                for (int i = 0; i < _callbackNodes.Count; i++)
                    _callbackNodes[i].Transform.GetLocalMatrix(out _nodeTransforms[_callbackNodes[i].NodeIndex], true);

            // handle blend sequences
            for (int i = firstBlend; i < _threadList.Count; i++)
            {
                Thread th = _threadList[i];
                if (th._blendDisabled)
                    continue;

                _HandleBlendSequence(th, a, b);
            }

            // multiply transforms...
            for (int i = a; i < b; i++)
            {
                int parentIdx = _shape.Nodes[i].ParentIndex;
                if (parentIdx >= 0)
                    _nodeTransforms[i] = Matrix.Multiply(_nodeTransforms[i], _nodeTransforms[parentIdx]);
            }
        }



        void _UpdateTransitions()
        {
            if (_transitionThreads.Count == 0)
                return;

            int numNodes = _shape.Nodes.Length;
            TorqueUtil.GrowArray<Quat16>(ref _nodeReferenceRotations, numNodes);
            TorqueUtil.GrowArray<Vector3>(ref _nodeReferenceTranslations, numNodes);
            for (int i = 0; i < numNodes; i++)
            {
                if (_transitionRotationNodes.Test(i))
                    _nodeReferenceRotations[i].Set(ShapeInstance._nodeCurrentRotations[i]);

                if (_transitionTranslationNodes.Test(i))
                    _nodeReferenceTranslations[i] = ShapeInstance._nodeCurrentTranslations[i];
            }

            if (AnimatesScale())
            {
                if (AnimatesUniformScale())
                {
                    TorqueUtil.GrowArray<float>(ref _nodeReferenceUniformScales, numNodes);
                    for (int i = 0; i < numNodes; i++)
                    {
                        if (_transitionScaleNodes.Test(i))
                            _nodeReferenceUniformScales[i] = ShapeInstance._nodeCurrentUniformScales[i];
                    }
                }
                else if (AnimatesAlignedScale())
                {
                    TorqueUtil.GrowArray<Vector3>(ref _nodeReferenceScaleFactors, numNodes);
                    for (int i = 0; i < numNodes; i++)
                    {
                        if (_transitionScaleNodes.Test(i))
                            _nodeReferenceScaleFactors[i] = ShapeInstance._nodeCurrentAlignedScales[i];
                    }
                }
                else
                {
                    TorqueUtil.GrowArray<Vector3>(ref _nodeReferenceScaleFactors, numNodes);
                    TorqueUtil.GrowArray<Quat16>(ref _nodeReferenceArbitraryScaleRots, numNodes);
                    for (int i = 0; i < numNodes; i++)
                    {
                        if (_transitionScaleNodes.Test(i))
                        {
                            _nodeReferenceScaleFactors[i] = ShapeInstance._nodeCurrentArbitraryScales[i].Scale;
                            _nodeReferenceArbitraryScaleRots[i].Set(ShapeInstance._nodeCurrentArbitraryScales[i].Rotate);
                        }
                    }
                }
            }

            // reset transition durations to account for new reference transforms
            for (int i = 0; i < _transitionThreads.Count; i++)
            {
                Thread th = _transitionThreads[i];
                if (th._transitionData._inTransition)
                {
                    th._transitionData._duration *= 1.0f - th._transitionData._pos;
                    th._transitionData._pos = 0.0f;
                }
            }
        }



        void _HandleDefaultScale(int a, int b, ref BitVector scaleBeenSet)
        {
            // Set default Scale values (i.e., SetIdentity) and do any initialization
            // relating to animated Scale (since Scale normally not animated)

            int numNodes = _shape.Nodes.Length;
            TorqueUtil.GrowArray<Thread>(ref ShapeInstance._workScaleThreads, numNodes);
            scaleBeenSet.TakeAway(_handsOffNodes);
            if (AnimatesUniformScale())
            {
                TorqueUtil.GrowArray<float>(ref ShapeInstance._nodeCurrentUniformScales, numNodes);
                for (int i = a; i < b; i++)
                    if (scaleBeenSet.Test(i))
                    {
                        ShapeInstance._nodeCurrentUniformScales[i] = 1.0f;
                        ShapeInstance._workScaleThreads[i] = null;
                    }
            }
            else if (AnimatesAlignedScale())
            {
                TorqueUtil.GrowArray<Vector3>(ref ShapeInstance._nodeCurrentAlignedScales, numNodes);
                for (int i = a; i < b; i++)
                    if (scaleBeenSet.Test(i))
                    {
                        ShapeInstance._nodeCurrentAlignedScales[i] = new Vector3(1.0f, 1.0f, 1.0f);
                        ShapeInstance._workScaleThreads[i] = null;
                    }
            }
            else
            {
                TorqueUtil.GrowArray<ArbitraryScale>(ref ShapeInstance._nodeCurrentArbitraryScales, numNodes);
                for (int i = a; i < b; i++)
                    if (scaleBeenSet.Test(i))
                    {
                        _nodeCurrentArbitraryScales[i].SetIdentity();
                        _workScaleThreads[i] = null;
                    }
            }

            scaleBeenSet.Overlap(_handsOffNodes);
        }



        void _HandleTransitionNodes(int a, int b)
        {
            // handle rotation
            int nodeIndex;
            int start = _transitionRotationNodes.Start();
            int end = b;
            for (nodeIndex = start; nodeIndex < end; _transitionRotationNodes.Next(ref nodeIndex))
            {
                if (nodeIndex < a)
                    continue;
                Thread thread = ShapeInstance._workRotationThreads[nodeIndex];
                if (thread == null || thread._transitionData._inTransition)
                {
                    // if not controlled by a _sequence in transition then there must be
                    // some other thread out there that used to control us that is in
                    // transition now...use that thread to control interpolation
                    for (int i = 0; i < _transitionThreads.Count; i++)
                    {
                        if (_transitionThreads[i]._transitionData._oldRotationNodes.Test(nodeIndex) || _transitionThreads[i].Sequence.DoesRotationMatter.Test(nodeIndex))
                        {
                            thread = _transitionThreads[i];
                            break;
                        }
                    }
                }

                if (thread != null)
                {
                    Quaternion tmpQ;
                    _nodeReferenceRotations[nodeIndex].Get(out tmpQ);
                    Transform.Interpolate(tmpQ, _nodeCurrentRotations[nodeIndex], thread._transitionData._pos, out _nodeCurrentRotations[nodeIndex]);
                }
            }

            // then translation
            start = _transitionTranslationNodes.Start();
            end = b;
            for (nodeIndex = start; nodeIndex < end; _transitionTranslationNodes.Next(ref nodeIndex))
            {
                Thread thread = _workTranslationThreads[nodeIndex];
                if (thread == null || thread._transitionData._inTransition)
                {
                    // if not controlled by a _sequence in transition then there must be
                    // some other thread out there that used to control us that is in
                    // transition now...use that thread to control interpolation
                    for (int i = 0; i < _transitionThreads.Count; i++)
                    {
                        if (_transitionThreads[i]._transitionData._oldTranslationNodes.Test(nodeIndex) || _transitionThreads[i].Sequence.DoesTranslationMatter.Test(nodeIndex))
                        {
                            thread = _transitionThreads[i];
                            break;
                        }
                    }
                }

                if (thread != null)
                {
                    Vector3 p1 = _nodeReferenceTranslations[nodeIndex];
                    Vector3 p2 = _nodeCurrentTranslations[nodeIndex];
                    float k = thread._transitionData._pos;
                    _nodeCurrentTranslations[nodeIndex] = new Vector3(p1.X + k * (p2.X - p1.X), p1.Y + k * (p2.Y - p1.Y), p1.Z + k * (p2.Z - p1.Z));
                }
            }

            // then Scale...
            if (ScaleCurrentlyAnimated())
            {
                start = _transitionScaleNodes.Start();
                end = b;
                for (nodeIndex = start; nodeIndex < end; _transitionScaleNodes.Next(ref nodeIndex))
                {
                    Thread thread = _workScaleThreads[nodeIndex];
                    if (thread != null && thread._transitionData._inTransition)
                    {
                        // if not controlled by a _sequence in transition then there must be
                        // some other thread out there that used to control us that is in
                        // transition now...use that thread to control interpolation
                        for (int i = 0; i < _transitionThreads.Count; i++)
                        {
                            if (_transitionThreads[i]._transitionData._oldScaleNodes.Test(nodeIndex) || _transitionThreads[i].Sequence.DoesScaleMatter.Test(nodeIndex))
                            {
                                thread = _transitionThreads[i];
                                break;
                            }
                        }
                    }

                    if (thread != null)
                    {
                        if (AnimatesUniformScale())
                            _nodeCurrentUniformScales[nodeIndex] += thread._transitionData._pos * (_nodeReferenceUniformScales[nodeIndex] - _nodeCurrentUniformScales[nodeIndex]);
                        else if (AnimatesAlignedScale())
                            Transform.Interpolate(_nodeReferenceScaleFactors[nodeIndex], _nodeCurrentAlignedScales[nodeIndex], thread._transitionData._pos, out _nodeCurrentAlignedScales[nodeIndex]);
                        else
                        {
                            Transform.Interpolate(_nodeReferenceScaleFactors[nodeIndex], _nodeCurrentArbitraryScales[nodeIndex].Scale, thread._transitionData._pos, out _nodeCurrentArbitraryScales[nodeIndex].Scale);
                            Quaternion q;
                            _nodeReferenceArbitraryScaleRots[nodeIndex].Get(out q);
                            Transform.Interpolate(q, _nodeCurrentArbitraryScales[nodeIndex].Rotate, thread._transitionData._pos, out _nodeCurrentArbitraryScales[nodeIndex].Rotate);
                        }
                    }
                }
            }
        }



        void _HandleNodeScale(int a, int b)
        {
            if (AnimatesUniformScale())
            {
                for (int i = a; i < b; i++)
                {
                    if (!_handsOffNodes.Test(i))
                        Transform.ApplyScale(_nodeCurrentUniformScales[i], ref _nodeTransforms[i]);
                }
            }
            else if (AnimatesAlignedScale())
            {
                for (int i = a; i < b; i++)
                {
                    if (!_handsOffNodes.Test(i))
                        Transform.ApplyScale(_nodeCurrentAlignedScales[i], ref _nodeTransforms[i]);
                }
            }
            else
            {
                for (int i = a; i < b; i++)
                {
                    if (!_handsOffNodes.Test(i))
                        Transform.ApplyScale(_nodeCurrentArbitraryScales[i], ref _nodeTransforms[i]);
                }
            }
        }



        void _HandleAnimatedScale(Thread thread, int a, int b, ref BitVector scaleBeenSet)
        {
            int j = 0;
            int start = thread.Sequence.DoesScaleMatter.Start();
            int end = b;

            // code the Scale conversion (might need to "upgrade" from uniform to arbitrary, e.g.)
            // code uniform, aligned, and arbitrary as 0,1, and 2, respectively,
            // with _sequence coding in first two bits, shape coding in Next two bits
            int code = 0;
            if (thread.Sequence.IsAlignedScaleAnimated())
                code += 1;
            else if (thread.Sequence.IsArbitraryScaleAnimated())
                code += 2;

            if (AnimatesAlignedScale())
                code += 3;

            if (AnimatesArbitraryScale())
                code += 6;

            float uniformScale = 1.0f;
            Vector3 alignedScale = new Vector3(1, 1, 1);
            ArbitraryScale arbitraryScale = new ArbitraryScale();
            for (int nodeIndex = start; nodeIndex < end; thread.Sequence.DoesScaleMatter.Next(ref nodeIndex), j++)
            {
                if (nodeIndex < a)
                    continue;

                if (!scaleBeenSet.Test(nodeIndex))
                {
                    // compute Scale in _sequence format
                    switch (code)
                    {
                        case 0: // uniform -> uniform
                        case 1: // uniform -> aligned
                        case 2: // uniform -> arbitrary
                            {
                                float s1 = _shape.GetUniformScale(thread.Sequence, thread._keyNum1, j);
                                float s2 = _shape.GetUniformScale(thread.Sequence, thread._keyNum2, j);
                                uniformScale = Transform.Interpolate(s1, s2, thread._keyPos);
                                alignedScale = new Vector3(uniformScale, uniformScale, uniformScale);
                                break;
                            }
                        case 4: // aligned -> aligned
                        case 5: // aligned -> arbitrary
                            {
                                Vector3 s1 = _shape.GetAlignedScale(thread.Sequence, thread._keyNum1, j);
                                Vector3 s2 = _shape.GetAlignedScale(thread.Sequence, thread._keyNum2, j);
                                Transform.Interpolate(s1, s2, thread._keyPos, out alignedScale);
                                break;
                            }
                        case 8: // arbitrary -> arbitary
                            {
                                ArbitraryScale s1, s2;
                                _shape.GetArbitraryScale(thread.Sequence, thread._keyNum1, j, out s1);
                                _shape.GetArbitraryScale(thread.Sequence, thread._keyNum2, j, out s2);
                                Transform.Interpolate(ref s1, ref s2, thread._keyPos, out arbitraryScale);
                                break;
                            }
                        default:
                            Assert.Fatal(false, "ShapeInstance.HandleAnimatedScale - Invalid sequence code.");
                            break;
                    }

                    switch (code)
                    {
                        case 0: // uniform -> uniform
                            {
                                _nodeCurrentUniformScales[nodeIndex] = uniformScale;
                                break;
                            }
                        case 1: // uniform -> aligned
                        case 4: // aligned -> aligned
                            {
                                _nodeCurrentAlignedScales[nodeIndex] = alignedScale;
                                break;
                            }
                        case 2: // uniform -> arbitrary
                        case 5: // aligned -> arbitrary
                            {
                                _nodeCurrentArbitraryScales[nodeIndex].SetIdentity();
                                _nodeCurrentArbitraryScales[nodeIndex].Scale = alignedScale;
                                break;
                            }
                        case 8: // arbitrary -> arbitary
                            {
                                _nodeCurrentArbitraryScales[nodeIndex] = arbitraryScale;
                                break;
                            }
                        default:
                            Assert.Fatal(false, "ShapeInstance.HandleAnimatedScale - Invalid sequence code.");
                            break;
                    }
                    _workScaleThreads[nodeIndex] = thread;
                    scaleBeenSet.Set(nodeIndex);
                }
            }
        }



        void _HandleBlendSequence(Thread thread, int a, int b)
        {
            int jrot = 0;
            int jtrans = 0;
            int jscale = 0;

            BitVector nodeMatters = new BitVector();
            nodeMatters.Copy(ref thread.Sequence.DoesTranslationMatter);
            nodeMatters.Overlap(thread.Sequence.DoesRotationMatter);
            nodeMatters.Overlap(thread.Sequence.DoesScaleMatter);
            int start = nodeMatters.Start();
            int end = b;
            for (int nodeIndex = start; nodeIndex < end; nodeMatters.Next(ref nodeIndex))
            {
                // skip nodes outside of this detail
                if (start < a || _disableBlendNodes.Test(nodeIndex))
                {
                    if (thread.Sequence.DoesRotationMatter.Test(nodeIndex))
                        jrot++;

                    if (thread.Sequence.DoesTranslationMatter.Test(nodeIndex))
                        jtrans++;

                    if (thread.Sequence.DoesScaleMatter.Test(nodeIndex))
                        jscale++;

                    continue;
                }

                Matrix mat = Matrix.Identity;

                if (thread.Sequence.DoesRotationMatter.Test(nodeIndex))
                {
                    Quaternion q1, q2;
                    _shape.GetRotation(thread.Sequence, thread._keyNum1, jrot, out q1);
                    _shape.GetRotation(thread.Sequence, thread._keyNum2, jrot, out q2);
                    Quaternion quat;
                    Transform.Interpolate(q1, q2, thread._keyPos, out quat);
                    Transform.SetMatrix(quat, out mat);
                    jrot++;
                }

                if (thread.Sequence.DoesTranslationMatter.Test(nodeIndex))
                {
                    Vector3 p1 = _shape.GetTranslation(thread.Sequence, thread._keyNum1, jtrans);
                    Vector3 p2 = _shape.GetTranslation(thread.Sequence, thread._keyNum2, jtrans);
                    Vector3 p;
                    Transform.Interpolate(p1, p2, thread._keyPos, out p);
                    mat.Translation = p;
                    jtrans++;
                }

                if (thread.Sequence.DoesScaleMatter.Test(nodeIndex))
                {
                    if (thread.Sequence.IsUniformScaleAnimated())
                    {
                        float s1 = _shape.GetUniformScale(thread.Sequence, thread._keyNum1, jscale);
                        float s2 = _shape.GetUniformScale(thread.Sequence, thread._keyNum2, jscale);
                        float scale = Transform.Interpolate(s1, s2, thread._keyPos);
                        Transform.ApplyScale(scale, ref mat);
                    }
                    else if (AnimatesAlignedScale())
                    {
                        Vector3 s1 = _shape.GetAlignedScale(thread.Sequence, thread._keyNum1, jscale);
                        Vector3 s2 = _shape.GetAlignedScale(thread.Sequence, thread._keyNum2, jscale);
                        Vector3 scale;
                        Transform.Interpolate(s1, s2, thread._keyPos, out scale);
                        Transform.ApplyScale(scale, ref mat);
                    }
                    else
                    {
                        ArbitraryScale s1, s2;
                        _shape.GetArbitraryScale(thread.Sequence, thread._keyNum1, jscale, out s1);
                        _shape.GetArbitraryScale(thread.Sequence, thread._keyNum2, jscale, out s2);
                        ArbitraryScale scale;
                        Transform.Interpolate(ref s1, ref s2, thread._keyPos, out scale);
                        Transform.ApplyScale(scale, ref mat);
                    }
                    jscale++;
                }

                // apply blend transform
                _nodeTransforms[nodeIndex] = Matrix.Multiply(mat, _nodeTransforms[nodeIndex]);
            }
        }



        void _CheckScaleCurrentlyAnimated()
        {
            _scaleCurrentlyAnimated = true;
            for (int i = 0; i < _threadList.Count; i++)
            {
                if (_threadList[i].Sequence.IsScaleAnimated())
                    return;
            }

            _scaleCurrentlyAnimated = false;
        }



        void _SetDirty(DirtyFlags dirty)
        {
            Assert.Fatal((dirty & DirtyFlags.AllDirtyMask) == dirty, "ShapeInstance._SetDirty - Illegal dirty flags.");
            for (int i = 0; i < _dirtyFlags.Length; i++)
                _dirtyFlags[i] |= dirty;
        }



        void _ClearDirty(DirtyFlags dirty)
        {
            Assert.Fatal((dirty & DirtyFlags.AllDirtyMask) == dirty, "ShapeInstance._ClearDirty - Illegal dirty flags.");
            for (int i = 0; i < _dirtyFlags.Length; i++)
                _dirtyFlags[i] &= ~dirty;
        }



        public void _SetTriggerState(int stateNum, bool on)
        {
            Assert.Fatal(stateNum <= 32 && stateNum > 0, "ShapeInstance._SetTriggerState - State index out of range.");

            stateNum--; // stateNum externally 1..32, internally 0..31
            int bit = 1 << stateNum;
            if (on)
                _triggerStates |= bit;
            else
                _triggerStates &= ~bit;
        }



        public void _SetTriggerStateBit(int stateBit, bool on)
        {
            if (on)
                _triggerStates |= stateBit;
            else
                _triggerStates &= ~stateBit;
        }

        #endregion
    }
}
