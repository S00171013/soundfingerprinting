﻿namespace SoundFingerprinting.Data
{
    using System;

    using SoundFingerprinting.DAO;

    [Serializable]
    public class SubFingerprintData
    {
        public SubFingerprintData()
        {
            // no op
        }

        public SubFingerprintData(byte[] signature, int sequenceNumber, IModelReference subFingerprintReference, IModelReference trackReference)
        {
            Signature = signature;
            SubFingerprintReference = subFingerprintReference;
            TrackReference = trackReference;
            SequenceNumber = sequenceNumber;
        }

        public byte[] Signature { get; set; }

        public int SequenceNumber { get; set; }

        [IgnoreBinding]
        public IModelReference SubFingerprintReference { get; set; }

        [IgnoreBinding]
        public IModelReference TrackReference { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is SubFingerprintData))
            {
                return false;
            }

            return ((SubFingerprintData)obj).SubFingerprintReference.Equals(SubFingerprintReference);
        }

        public override int GetHashCode()
        {
            return SubFingerprintReference.GetHashCode();
        }
    }
}
