using System;

namespace CBG.FPSMeshTool {
    [Serializable]
    public struct Include {
        public bool head;
        public bool body;
        public bool leftArm;
        public bool rightArm;
        public bool leftLeg;
        public bool rightLeg;

        private bool _all;
        
        public bool All {
            get { return _all; }
            set {
                if (value) {
                    head = true;
                    body = true;
                    leftArm = true;
                    rightArm = true;
                    leftLeg = true;
                    rightLeg = true;
                }
                _all = value;
            }
        }
        public Include(bool includeAll) {
            head = includeAll;
            body = includeAll;
            leftArm = includeAll;
            rightArm = includeAll;
            leftLeg = includeAll;
            rightLeg = includeAll;
            _all = includeAll;
        }
    }

    [Serializable]
    public struct MeshSettings {
        public float weightThreshold;
        public int vertexThreshold;
        public Include include;
        public bool separateArms;
        public bool processLegs;
        public bool separateLegs;

        public MeshSettings(float weightThreshold, int vertexThreshold) {
            this.weightThreshold = 1;
            this.vertexThreshold = 1;
            include = new Include(true);
            separateArms = false;
            processLegs = false;
            separateLegs = false;
        }
    }
}