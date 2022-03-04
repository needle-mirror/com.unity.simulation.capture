#if !UNITY_SIMULATION_SDK_DISABLED
#if UNITY_2019_3_OR_NEWER
using UnityEngine;
using UnityEngine.TestTools;

using NUnit.Framework;

using Unity.Simulation;

namespace Unity.Simulation
{
    class NameGeneratorTests
    {
        [Test]
        public void NameGenerator_ParsesPathCorrectly()
        {
            var generator = new NameGenerator
            (
                new DirectoryNameComponent(),
                new FileNameComponent(),
                new ExtensionNameComponent()
            );

            Assert.AreEqual("c:/some/directory/blah/somefile.txt", generator.Generate("c:\\some\\directory\\blah\\somefile.txt"));
            Assert.AreEqual("c:/some/directory/blah/somefile",     generator.Generate("c:\\some\\directory\\blah\\somefile"));
            Assert.AreEqual("/some/directory/blah/somefile.txt",   generator.Generate("/some/directory/blah/somefile.txt"));
            Assert.AreEqual("/some/directory/blah/somefile",       generator.Generate("/some/directory/blah/somefile"));
        }

        [Test]
        public void NameGenerator_SequenceIncrements()
        {
            var generator = new NameGenerator
            (
                new DirectoryNameComponent(),
                new FileNameComponent(),
                new SequenceNameComponent(prefix: "_"),
                new ExtensionNameComponent()
            );

            Assert.AreEqual("c:/some/directory/blah/somefile_0.txt", generator.Generate("c:\\some\\directory\\blah\\somefile.txt"));
            Assert.AreEqual("c:/some/directory/blah/somefile_1",     generator.Generate("c:\\some\\directory\\blah\\somefile"));
            Assert.AreEqual("/directory/blah/somefile_2.txt",        generator.Generate("/directory/blah/somefile.txt"));
            Assert.AreEqual("/directory/blah/somefile_3",            generator.Generate("/directory/blah/somefile"));
        }

        [Test]
        public void NameGenerator_All()
        {
            var generator = new NameGenerator
            (
                new DirectoryNameComponent(),
                new FileNameComponent(),
                new LabelNameComponent("BLAH", prefix: "_"),
                new TimestampNameComponent(prefix: "_", timerSource: TimerSource.Time),
                new FrameNumberNameComponent(prefix: "_"),
                new SequenceNameComponent(prefix: "_"),
                new ExtensionNameComponent()
            );

            var timer = new Timer { timerSource = TimerSource.Time };
            var ts = timer.elapsedSeconds.ToString("F0");
            var frame = Time.frameCount;
 
            Assert.AreEqual($"c:/some/directory/blah/somefile_BLAH_{ts}_{frame}_0.txt", generator.Generate("c:\\some\\directory\\blah\\somefile.txt"));
            Assert.AreEqual($"c:/some/directory/blah/somefile_BLAH_{ts}_{frame}_1",     generator.Generate("c:\\some\\directory\\blah\\somefile"));
            Assert.AreEqual($"/directory/blah/somefile_BLAH_{ts}_{frame}_2.txt",        generator.Generate("/directory/blah/somefile.txt"));
            Assert.AreEqual($"/directory/blah/somefile_BLAH_{ts}_{frame}_3",            generator.Generate("/directory/blah/somefile"));
        }
    }
}
#endif // UNITY_2019_3_OR_NEWER
#endif // !UNITY_SIMULATION_SDK_DISABLED
