using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;


namespace PlatformerStarter
{
    public class JukeBox
    {
        private SoundEffect backgroundMusic;
        private SoundEffectInstance musicInstance;
        //private WaveBank waveBank;
        //private SoundBank soundBank;

        public JukeBox(string musicFilename, ContentManager content)//string xactProject, string waveBankFile, string soundBankFile)
        {
            backgroundMusic = content.Load<SoundEffect>(musicFilename);
            musicInstance = backgroundMusic.CreateInstance();
            musicInstance.IsLooped = true;

            /*engine = new AudioEngine(xactProject);
            waveBank = new WaveBank(engine, waveBankFile);
            soundBank = new SoundBank(engine, soundBankFile);*/
        }

        public void RunJukeBox()
        {
            musicInstance.Play();
            //engine.Update();
        }

        public void PlayMusic(string song)
        {
            //soundBank.PlayCue(song);
            // Random Comment to test git
        }
    }
}
