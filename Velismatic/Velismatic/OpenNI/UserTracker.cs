using System;
using System.Collections.Generic;
using OpenNI;
using System.Threading.Tasks;
using System.Threading;

namespace Velismatic.OpenNI
{
    public class UserTracker : IDisposable
    {
        private readonly string SAMPLE_XML_FILE = @"SamplesConfig.xml";

        private Context context;
        private DepthGenerator depth;
        private UserGenerator userGenerator;
        private SkeletonCapability skeletonCapbility;
        private Dictionary<int, Dictionary<SkeletonJoint, SkeletonJointPosition>> joints;
        private bool shouldUpdate;

        public UserTracker()
        {
            this.Initialize();
        }

        private void Initialize()
        {
            ScriptNode scriptNode;
            this.context = Context.CreateFromXmlFile(SAMPLE_XML_FILE, out scriptNode);
            this.depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
            if (this.depth == null)
            {
                throw new Exception("Viewer must have a depth node!");
            }
            this.userGenerator = new UserGenerator(this.context);
            this.skeletonCapbility = this.userGenerator.SkeletonCapability;
            this.userGenerator.NewUser += this.OnUserGeneratorNewUser;
            this.userGenerator.LostUser += this.OnUserGeneratorLostUser;
            this.skeletonCapbility.CalibrationComplete += this.OnSkeletonCapbilityCalibrationComplete;
            this.skeletonCapbility.SetSkeletonProfile(SkeletonProfile.All);
            this.joints = new Dictionary<int, Dictionary<SkeletonJoint, SkeletonJointPosition>>();
            this.userGenerator.StartGenerating();
        }

        private void OnSkeletonCapbilityCalibrationComplete(object sender, CalibrationProgressEventArgs e)
        {
            if (e.Status == CalibrationStatus.OK)
            {
                this.skeletonCapbility.StartTracking(e.ID);
                this.joints.Add(e.ID, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
            }
            else if (e.Status != CalibrationStatus.ManualAbort)
            {
                this.skeletonCapbility.RequestCalibration(e.ID, true);
            }
        }

        private void OnUserGeneratorNewUser(object sender, NewUserEventArgs e)
        {
            this.skeletonCapbility.RequestCalibration(e.ID, true);
        }

        private void OnUserGeneratorLostUser(object sender, UserLostEventArgs e)
        {
            this.joints.Remove(e.ID);
        }

        public void UpdateInBackground()
        {
            this.shouldUpdate = true;
            Task.Factory.StartNew(() =>
            {
                while (this.shouldUpdate)
                {
                    try
                    {
                        this.context.WaitOneUpdateAll(this.depth);
                    }
                    catch (Exception) { }
                    int[] users = this.userGenerator.GetUsers();
                    foreach (int user in users)
                    {
                        if (this.skeletonCapbility.IsTracking(user))
                        {
                            GetJoints(user);
                        }
                    }
                }
            });
        }

        private void GetJoint(int user, SkeletonJoint joint)
        {
            SkeletonJointPosition pos = this.skeletonCapbility.GetSkeletonJointPosition(user, joint);
            if (pos.Position.Z == 0)
            {
                pos.Confidence = 0;
            }
            else
            {
                pos.Position = this.depth.ConvertRealWorldToProjective(pos.Position);
            }
            this.joints[user][joint] = pos;
        }

        private void GetJoints(int user)
        {
            GetJoint(user, SkeletonJoint.Head);
            GetJoint(user, SkeletonJoint.LeftHand);
            GetJoint(user, SkeletonJoint.RightHand);
            if (this.JointsUpdatedCallback != null)
            {
                this.JointsUpdatedCallback();
            }
        }

        public Dictionary<int, Dictionary<SkeletonJoint, SkeletonJointPosition>> Joints
        {
            get
            {
                return this.joints;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UserTracker()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.shouldUpdate = false;
                Thread.Sleep(500);
                this.userGenerator.Dispose();
                this.skeletonCapbility.Dispose();
                this.depth.Dispose();
                this.context.Dispose();
            }
        }
    
        public Action JointsUpdatedCallback { get; set; }
    }
}
