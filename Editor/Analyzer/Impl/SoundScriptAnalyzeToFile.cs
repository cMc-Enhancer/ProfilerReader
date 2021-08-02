using System.Collections.Generic;

namespace UTJ.ProfilerReader.Analyzer
{
    public class SoundScriptAnalyzeToFile : AbstractMethodSampleAnalyzer
    {

        private static readonly List<string> MethodPatterns = new List<string>
        {
            "AkAudioListener.Update()",
            "AkGameObj.OnTriggerExit()",
            "ImpactObjectRigidbody.FixedUpdate()",
            "ImpactCollisionTrigger3D.OnCollisionEnter()",
            "ImpactManager.FixedUpdate()",
            "ImpactSlideAndRollTrigger3D.OnCollisionStay()",
            "CharacterSoundSound.FixedUpdate()",
            "SoundManager.FixedUpdate()",
            "WeaponSoundProjectile.FixedUpdate()",
            "AkGameObj",
            "WeaponSoundHammer",
            "WeaponSoundPlunger",
            "WeaponSoundCrossBow",
            "WeaponSoundEnergyDrink",
            "WeaponSoundBase",
            "WeaponSoundNunchucks",
            "WeaponSoundBat",
        };
        
        protected override bool IsSampleNameMatched(string name)
        {
            foreach (var method in MethodPatterns)
            {
                if (name.Contains(method))
                {
                    return true;
                }
            }

            return false;
        }

        protected override string FooterName
        {
            get { return "_sound_method_per_frame.csv"; }
        }
    }
}