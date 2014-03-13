using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Gw2MappingLink
{
    class MumbleLink
    {
        private MemoryMappedFile mmFile;
        private MemoryMappedViewAccessor mmAccessor;

        public unsafe struct MemoryMap
        {
            public uint uiVersion;
            public uint uiTick;
            public fixed float fAvatarPosition[3];
            public fixed float fAvatarFront[3];
            public fixed float fAvatarTop[3];
            public fixed byte name[512];
            public fixed float fCameraPosition[3];
            public fixed float fCameraFront[3];
            public fixed float fCameraTop[3];
            public fixed byte identity[512];
            public uint context_len;
            public fixed byte context[512];
            public fixed byte description[4096];
        };

        public MemoryMap data = new MemoryMap();

        public bool parse()
        {
            using (mmFile = MemoryMappedFile.CreateOrOpen("MumbleLink", Marshal.SizeOf(data)))
            {
                if (mmFile != null)
                {
                    using (mmAccessor = mmFile.CreateViewAccessor(0, Marshal.SizeOf(data)))
                    {
                        mmAccessor.Read(0, out data);
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
