#if !UNITY_SIMULATION_SDK_DISABLED
namespace Unity.Simulation
{
    /// <summary>
    /// Various options that can be configured for the Core package.
    /// </summary>
    public static class Options
    {
        /// <summary>
        /// When files are uploaded, they are removed from the local file system.
        /// Setting this to false will cause those files to be left alone.
        /// </summary>
        public static bool removeLocalFilesAfterUpload = true;

        /// <summary>
        /// When starting, the Manager looks to see if any files are present on
        /// the file system from a previous run, and will attempt to upload this.
        /// Setting this to false will disable that.
        /// </summary>
        public static bool uploadFilesFromPreviousRun = true;

        /// <summary>
        /// Debugging option to not write files to the file system, but appear as if they were.
        /// Useful for debugging when you don't want to spam the file system with tons of data.
        /// </summary>
        public static bool debugDontWriteFiles = false;

        /// <summary>
        /// When shutting down, we can wait to complete any outstanding requests.
        /// However if there are any requests that have been created, but not
        /// started yet, we would need to wait for those before completing.
        /// This option specifies the number of frames to wait before just
        /// going ahead and completing what has been started.
        /// Requests that are completed without starting will lose their workloads.
        /// Not supported in editor when exiting playmode.
        /// </summary>
        [System.Obsolete("This property is obsolete. The functionality that used it has been removed.", false)]
        public static int  maxRequestStartFramesToWait = 0;
    }
}
#endif // !UNITY_SIMULATION_SDK_DISABLED
