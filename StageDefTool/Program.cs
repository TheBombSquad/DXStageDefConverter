// This is a tool for converting little endian Super Monkey Ball Deluxe stagedefs to big endian Super Monkey Ball 2 stagedefs. 
// This is my first legitimate project in C# - many implementations here are obviously shitty in retrospect
// Therefore, I suggest you don't use any of this code as a reference for how to do literally anything efficiently
// A much more efficient way of working with StageDefs (including DX->SMB2 conversion) is currently in the works
// See CraftedCart's amazing documentation of the SMB2 StageDef format for stagedef format references:
// Link: https://craftedcart.github.io/SMBLevelWorkshop/documentation/index.html?page=stagedefFormat2
// There are some inaccuracies with this documentation, however. The 'mystery 8' structure and the effect type 1 are not currently accurately documented.
// Here are my best guesses as to the actual structure:

/*Mystery 8 - Foreground Object, length 0x38
    Sample foreground object is from STAGE801 - Plain, the ladybug with X scaling on the bug modified
        0000001F - 4 byte integer    - unknown number, most seem to be 0x1F, I've seen 0x07 and 0x0F as well
        00021059 - 4 byte integer    - offset to model name
        00000000 - 4 byte unknown    - unknown, have not seen non-null value
        C119E148 - 4 byte float      - x position
        3F477DE9 - 4 byte float      - y position
        4080FF97 - 4 byte float      - z position
        025D     - 2 byte short      - x rotation (standard 0 to FFFF rotation found in stagedef)
        0AD6     - 2 byte short      - y rotation ''
        045A     - 2 byte short      - z rotation ''
        0000     - 2 byte short      - unknown, have not seen non-null value
        40F00000 - 4 byte float      - x scaling
        3F800000 - 4 byte float      - y scaling
        3F800000 - 4 byte float      - z scaling
        00000000 - 4 byte unknown    - unknown, have not seen non-null value
        00010CA0 - 4 byte integer    - offset to animation header
        00000000 - 4 byte unknown    - unknown, have not seen non-null value*/

/*Effect type 1, length: 0x14
    Sample effect is silhoutte from a diner BG object in the SMBDX version of Conveyers
        4208E80C - 4 byte float?    - seems to change every keyframe
        C0940013 - 4 byte float?    - seems to change every keyframe
        C2A73500 - 4 byte float?    - seems to change every keyframe
        0000     - 2 byte short?    - have not seen non-null values
        E03A     - 2 byte short?    - seems to change for some keyframes
        0000     - 2 byte short?    - have not seen non-null values
        41       - 1 byte           - appears to increment by 1, or otherwise changes value for each new keyframe.
        00       - 1 byte           - have not seen non-null values*/


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace StageDefTool
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Invalid number of arguments!");
                Console.WriteLine("Syntax: DXStageDefConverter.exe [input filename].lz.raw");
                return;
            }

            var standardOutput = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };

            Console.WriteLine("Attempting conversion...");

            // code for outputting foreground object information is associated with mys8test, uncomment to do so
            // String[] filePathList = Directory.GetFiles(args[0]);
            // using var mys8test = new BinaryWriter(File.Open("mys8test.txt", FileMode.OpenOrCreate));

            //foreach (String file in filePathList)
            //{
            //    args[0] = file;

            try
            {
                using var writer = new BinaryWriter(File.Open(args[0] + ".out", FileMode.OpenOrCreate));
                using var reader = new BinaryReader(File.Open(args[0], FileMode.Open));
                var output = new FileStream(args[0] + ".out.txt", FileMode.OpenOrCreate);
                var streamwriter = new StreamWriter(output)
                {
                    AutoFlush = true
                };

                Console.SetOut(streamwriter);
                Console.SetError(streamwriter);

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                var fileSize = (int)new FileInfo(args[0]).Length;
                var byteBuffer = new List<byte>();

                var collisionHeaders = new offsetCount();
                uint startPosOffset;
                uint falloutPosOffset;
                var goals = new offsetCount();
                var bumpers = new offsetCount();
                var jamabars = new offsetCount();
                var bananas = new offsetCount();
                var cone = new offsetCount();
                var sphere = new offsetCount();
                var cyl = new offsetCount();
                var fallout = new offsetCount();
                var bgModel = new offsetCount();
                var mys8 = new offsetCount();
                var reflect = new offsetCount();
                var levelModelInst = new offsetCount();
                var levelModelA = new offsetCount();
                var levelModelB = new offsetCount();
                var switches = new offsetCount();
                uint fogAnimOffset;
                var wormhole = new offsetCount();
                uint fogOffset;
                uint mys3;
                var EndCheck = new List<byte>();
                offsetCount currentColGrid;
                uint maxTriangleIndex = 0;
                var keyframeTotal = 0;
                // stagedef identifier
                reader.ReadBytes(8);

                // reads in header 
                collisionHeaders.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                collisionHeaders.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                startPosOffset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                falloutPosOffset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                goals.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                goals.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                bumpers.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                bumpers.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                jamabars.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                jamabars.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                bananas.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                bananas.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                cone.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                cone.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                sphere.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                sphere.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                cyl.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                cyl.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                fallout.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                fallout.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                bgModel.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                bgModel.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                mys8.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                mys8.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                // unknown/null
                reader.ReadBytes(8);
                reflect.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                reflect.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                // unknown/null
                reader.ReadBytes(12);
                levelModelInst.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                levelModelInst.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                levelModelA.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                levelModelA.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                levelModelB.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                levelModelB.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                // unknown/null
                reader.ReadBytes(12);
                switches.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                switches.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                fogAnimOffset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                wormhole.count = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                wormhole.offset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                fogOffset = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);
                // unknown/null
                reader.ReadBytes(20);
                mys3 = BitConverter.ToUInt32(BitConverter.GetBytes(reader.ReadInt32()), 0);

                // reset to start of file
                reader.BaseStream.Position = 0;
                // converts header
                Console.WriteLine("Converting stagedef header...");
                endianConvert(byteBuffer, 4, 0x227);


                var headerList = new collisionHeader[collisionHeaders.count];
                reader.BaseStream.Position = collisionHeaders.offset;
                writer.BaseStream.Position = collisionHeaders.offset;

                // reads in the collision headers 
                for (var i = 0; i < collisionHeaders.count; i++)
                {
                    reader.BaseStream.Position = collisionHeaders.offset + i * 0x49C;
                    writer.BaseStream.Position = collisionHeaders.offset + i * 0x49C;

                    Console.WriteLine("Converting collision header " + i);
                    object colHeader = headerList[i];
                    foreach (var field in typeof(collisionHeader).GetFields())
                    {
                        if (field.FieldType == typeof(Vector3))
                        {
                            if (field.Name.EndsWith("F"))
                                field.SetValue(colHeader, readVector3(typeof(float)));

                            else
                                field.SetValue(colHeader, readVector3(typeof(ushort)));
                        }

                        else if (field.FieldType == typeof(Vector2))
                        {
                            if (field.Name.EndsWith("F"))
                                field.SetValue(colHeader, readVector2(typeof(float)));

                            else
                                field.SetValue(colHeader, readVector2(typeof(uint)));
                        }

                        else if (field.FieldType == typeof(offsetCount))
                        {
                            field.SetValue(colHeader, readOffsetCount());
                        }

                        else if (field.FieldType == typeof(float))
                        {
                            field.SetValue(colHeader,
                                BitConverter.ToSingle(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0));
                        }
                        else if (field.FieldType == typeof(ushort))
                        {
                            field.SetValue(colHeader,
                                BitConverter.ToUInt16(endianConvert(byteBuffer, 2, 1, false).ToArray(), 0));
                        }
                        else if (field.FieldType == typeof(ulong))
                        {
                            field.SetValue(colHeader,
                                BitConverter.ToUInt64(endianConvert(byteBuffer, 8, 1, false).ToArray(), 0));
                        }
                        else if (field.FieldType == typeof(uint))
                        {
                            field.SetValue(colHeader,
                                BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0));
                        }


                        Console.WriteLine("\tConverting part of collision header: " + field + " = " +
                                          field.GetValue(colHeader));
                    }

                    headerList[i] = (collisionHeader)colHeader;

                    reader.BaseStream.Position = headerList[i].animationHeaderOffset;
                    writer.BaseStream.Position = headerList[i].animationHeaderOffset;

                    // Reads animation header

                    if (headerList[i].animationHeaderOffset != 0)
                    {
                        Console.WriteLine("\tConverting animation header...");

                        reader.BaseStream.Position = headerList[i].animationHeaderOffset;
                        writer.BaseStream.Position = headerList[i].animationHeaderOffset;

                        // iterates through Xrot, Yrot, Zrot, X, Y, Z translation offsets and converts them
                        convertAnimationHeader(44, 0);
                    }

                    // collision grid stuff
                    ;
                    currentColGrid.count = (uint)headerList[i].collisionGridStepCount.X *
                                           (uint)headerList[i].collisionGridStepCount.Y;
                    currentColGrid.offset = headerList[i].collisionGridTriangleListOffset;

                    Console.WriteLine("\tConverting triangle index list offsets...");
                    // gets list of triangle offsets
                    var triangleListOffsets = new List<uint>();
                    for (var k = 0; k < currentColGrid.count; k++)
                    {
                        reader.BaseStream.Position = currentColGrid.offset + k * 4;
                        writer.BaseStream.Position = currentColGrid.offset + k * 4;
                        var offsetToAdd = BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);

                        if (offsetToAdd != 0) triangleListOffsets.Add(offsetToAdd);
                    }

                    Console.WriteLine("\tConverting triangle list (length: " + triangleListOffsets.Count + ")...");
                    // converts triangle lists and gets highest triangle index
                    foreach (var offset in triangleListOffsets)
                    {
                        reader.BaseStream.Position = offset;
                        writer.BaseStream.Position = offset;

                        uint triangleIndex;
                        do
                        {
                            triangleIndex = BitConverter.ToUInt16(endianConvert(byteBuffer, 2, 1, false).ToArray(), 0);
                            if (triangleIndex > maxTriangleIndex && triangleIndex != 0xFFFF)
                                maxTriangleIndex = triangleIndex;
                        } while (triangleIndex != 0xFFFF);
                    }

                    Console.WriteLine("\tConverting triangles (count: " + maxTriangleIndex + ")...");
                    // converts triangles
                    if (maxTriangleIndex != 0)
                    {
                        reader.BaseStream.Position = headerList[i].collisionTriangleListOffset;
                        writer.BaseStream.Position = headerList[i].collisionTriangleListOffset;
                        for (var l = 0; l < maxTriangleIndex + 1; l++)
                        {
                            endianConvert(byteBuffer, 4, 6);
                            endianConvert(byteBuffer, 2, 4);
                            endianConvert(byteBuffer, 4, 8);
                        }
                    }

                    convertGoal(headerList[i].goals);
                    convertBumper(headerList[i].bumpers);
                    convertJamabars(headerList[i].jamabars);
                    convertBananas(headerList[i].bananas);
                    convertCone(headerList[i].cone);
                    convertSphere(headerList[i].sphere);
                    convertCylinder(headerList[i].cyl);
                    convertFallout(headerList[i].fallout);
                    convertReflective(headerList[i].reflective);
                    convertLevelModelInstance(headerList[i].levelModelInst);
                    convertLevelModelB(headerList[i].levelModelB);
                    convertSwitches(headerList[i].switches);
                    convertWormholes(headerList[i].wormhole);
                    convertMys5(headerList[i].offsetMys5);

                    reader.BaseStream.Position = headerList[i].textureScrollOffset; // converts texture scroll
                    writer.BaseStream.Position = reader.BaseStream.Position;
                    if (reader.BaseStream.Position != 0)
                    {
                        Console.WriteLine("\tConverting texture scroll...");
                        endianConvert(byteBuffer, 4, 2);
                    }
                    maxTriangleIndex = 0;
                }


                //startpos
                reader.BaseStream.Position = startPosOffset;
                writer.BaseStream.Position = startPosOffset;
                endianConvert(byteBuffer, 4, 3);
                endianConvert(byteBuffer, 2, 4);

                //falloutpos
                reader.BaseStream.Position = falloutPosOffset;
                writer.BaseStream.Position = falloutPosOffset;
                endianConvert(byteBuffer, 4, 1);

                // header information
                Console.WriteLine("Now converting information in the header...");
                convertGoal(goals, true);
                convertBumper(bumpers, true);
                convertJamabars(jamabars, true);
                convertBananas(bananas, true);
                convertCone(cone, true);
                convertSphere(sphere, true);
                convertCylinder(cyl, true);
                convertFallout(fallout, true);
                convertBGModel(bgModel);
                convertMys8(mys8);
                convertReflective(reflect, true);
                convertLevelModelInstance(levelModelInst, true);
                convertLevelModelA(levelModelA, true);
                convertLevelModelB(levelModelB, true);
                convertSwitches(switches, true);

                // fog animation header            
                if (fogAnimOffset != 0)
                {
                    Console.WriteLine("Converting fog animation...");
                    reader.BaseStream.Position = fogAnimOffset;
                    writer.BaseStream.Position = fogAnimOffset;
                    convertAnimationHeader(44, 0);
                }

                convertWormholes(wormhole, true);

                // fog effect
                if (fogOffset != 0)
                {
                    Console.WriteLine("Converting fog effects...");
                    reader.BaseStream.Position = fogOffset;
                    writer.BaseStream.Position = fogOffset;

                    // FIX THIS!!!!!!! 
                    writer.Write(reader.ReadBytes(0x18));
                }

                // mystery 3 
                if (mys3 != 0)
                {
                    Console.WriteLine("Converting mystery 3...");
                    reader.BaseStream.Position = mys3;
                    writer.BaseStream.Position = mys3;

                    endianConvert(byteBuffer, 4, 3);
                    endianConvert(byteBuffer, 2, 2);
                    endianConvert(byteBuffer, 4, 5);
                }
                stopWatch.Stop();
                Console.WriteLine("\nDone in " + stopWatch.ElapsedMilliseconds / (float)1000 + "s!");
                Console.WriteLine("Keyframes converted: " + keyframeTotal);

                Console.SetOut(standardOutput);
                Console.WriteLine("Keyframe and stagedef data written to output.txt\n");

                List<byte> endianConvert(List<byte> byteBuffer, int byteCount, int numberOfTimes,
                    bool clearAfter = true)
                {
                    byteBuffer.Clear();
                    for (var i = 0; i < numberOfTimes; i++)
                    {
                        for (var j = 0; j < byteCount; j++) byteBuffer.Add(reader.ReadByte());
                        byteBuffer.Reverse();
                        writer.Write(byteBuffer.ToArray());
                        if (clearAfter) byteBuffer.Clear();
                    }

                    byteBuffer.Reverse();
                    return byteBuffer;
                }

                Vector3? readVector3(Type type)
                {
                    Vector3 tempVector;
                    if (type == typeof(float))
                    {
                        tempVector.X = BitConverter.ToSingle(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                        tempVector.Y = BitConverter.ToSingle(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                        tempVector.Z = BitConverter.ToSingle(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                        return tempVector;
                    }

                    if (type == typeof(uint))
                    {
                        tempVector.X = BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                        tempVector.Y = BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                        tempVector.Z = BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                        return tempVector;
                    }

                    if (type == typeof(ushort))
                    {
                        tempVector.X = BitConverter.ToUInt16(endianConvert(byteBuffer, 2, 1, false).ToArray(), 0);
                        tempVector.Y = BitConverter.ToUInt16(endianConvert(byteBuffer, 2, 1, false).ToArray(), 0);
                        tempVector.Z = BitConverter.ToUInt16(endianConvert(byteBuffer, 2, 1, false).ToArray(), 0);
                        return tempVector;
                    }

                    return null;
                }

                Vector2? readVector2(Type type)
                {
                    Vector2 tempVector;
                    if (type == typeof(float))
                    {
                        tempVector.X = BitConverter.ToSingle(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                        tempVector.Y = BitConverter.ToSingle(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                        return tempVector;
                    }

                    if (type == typeof(uint))
                    {
                        tempVector.X = BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                        tempVector.Y = BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                        return tempVector;
                    }

                    if (type == typeof(ushort))
                    {
                        tempVector.X = BitConverter.ToUInt16(endianConvert(byteBuffer, 2, 1, false).ToArray(), 0);
                        tempVector.Y = BitConverter.ToUInt16(endianConvert(byteBuffer, 2, 1, false).ToArray(), 0);
                        return tempVector;
                    }

                    return null;
                }

                offsetCount? readOffsetCount()
                {
                    var tempOffsetCount = new offsetCount
                    {
                        count = BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0),
                        offset = BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0)
                    };
                    return tempOffsetCount;
                }

                void convertGoal(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != goals.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting goals (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x14;
                            writer.BaseStream.Position = oc.offset + i * 0x14;
                            endianConvert(byteBuffer, 4, 3);
                            endianConvert(byteBuffer, 2, 3);
                            // goal type is stupid
                            writer.Write(reader.ReadBytes(2));
                        }
                    }
                }

                void convertBumper(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != bumpers.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting bumpers (" + oc.count + "at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x20;
                            writer.BaseStream.Position = oc.offset + i * 0x20;
                            endianConvert(byteBuffer, 4, 3);
                            endianConvert(byteBuffer, 2, 4);
                            endianConvert(byteBuffer, 4, 3);
                        }
                    }
                }

                void convertJamabars(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != jamabars.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting jamabars (" + oc.count + "at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x20;
                            writer.BaseStream.Position = oc.offset + i * 0x20;
                            endianConvert(byteBuffer, 4, 3);
                            endianConvert(byteBuffer, 2, 4);
                            endianConvert(byteBuffer, 4, 3);
                        }
                    }
                }

                void convertBananas(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != bananas.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting bananas (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x10;
                            writer.BaseStream.Position = oc.offset + i * 0x10;
                            endianConvert(byteBuffer, 4, 4);
                        }
                    }
                }

                void convertCone(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != cone.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting cone collision objects (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x20;
                            writer.BaseStream.Position = oc.offset + i * 0x20;
                            endianConvert(byteBuffer, 4, 3);
                            endianConvert(byteBuffer, 2, 4);
                            endianConvert(byteBuffer, 4, 3);
                        }
                    }
                }

                void convertSphere(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != sphere.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting sphere collision objects (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x14;
                            writer.BaseStream.Position = oc.offset + i * 0x14;
                            endianConvert(byteBuffer, 4, 5);
                        }
                    }
                }

                void convertCylinder(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != cyl.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting cylinder collision objects (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x1C;
                            writer.BaseStream.Position = oc.offset + i * 0x1C;
                            endianConvert(byteBuffer, 4, 5);
                            endianConvert(byteBuffer, 2, 4);
                        }
                    }
                }

                void convertFallout(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != fallout.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting fallout volumes (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x20;
                            writer.BaseStream.Position = oc.offset + i * 0x20;
                            endianConvert(byteBuffer, 4, 6);
                            endianConvert(byteBuffer, 2, 4);
                        }
                    }
                }

                void convertBGModel(offsetCount oc)
                {
                    if (oc.offset != 0)
                    {
                        Console.WriteLine("Converting background objects (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            Console.WriteLine("Converting background object " + i + " at 0x" +
                                              string.Format("{0:X8}", oc.offset + i * 0x38) + "...");
                            reader.BaseStream.Position = oc.offset + i * 0x38;
                            writer.BaseStream.Position = oc.offset + i * 0x38;

                            endianConvert(byteBuffer, 4, 1);

                            reader.BaseStream.Position =
                                BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                            writer.BaseStream.Position = reader.BaseStream.Position;

                            Console.Write("\t");
                            byte modelNameByte;
                            do
                            {
                                modelNameByte = reader.ReadByte();
                                writer.Write(modelNameByte);
                                Console.Write((char)modelNameByte);
                            } while (modelNameByte != 00);

                            Console.Write("\n");

                            reader.BaseStream.Position = oc.offset + i * 0x38 + 0x8;
                            writer.BaseStream.Position = oc.offset + i * 0x38 + 0x8;
                            endianConvert(byteBuffer, 4, 4);
                            endianConvert(byteBuffer, 2, 4);
                            endianConvert(byteBuffer, 4, 3);

                            // converts anim header 1
                            reader.BaseStream.Position =
                                BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                            writer.BaseStream.Position = reader.BaseStream.Position;

                            if (reader.BaseStream.Position != 0)
                            {
                                Console.WriteLine("Converting animation header 1 (" + "at 0x" +
                                                  string.Format("{0:X8}", reader.BaseStream.Position) + ")...");
                                endianConvert(byteBuffer, 4, 4);
                                convertAnimationHeader(64, 0);
                            }

                            // converts anim header 2
                            reader.BaseStream.Position = oc.offset + i * 0x38 + 0x30;
                            writer.BaseStream.Position = reader.BaseStream.Position;
                            reader.BaseStream.Position =
                                BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                            writer.BaseStream.Position = reader.BaseStream.Position;

                            if (reader.BaseStream.Position != 0)
                            {
                                Console.WriteLine("Converting animation header 2 (" + "at 0x" +
                                                  string.Format("{0:X8}", reader.BaseStream.Position) + ")...");
                                endianConvert(byteBuffer, 4, 2);    // converts unknown/null 1, and animation loop point
                                convertAnimationHeader(0x50, 0);    // converts the rest of the list
                            }

                            // converts effect header
                            reader.BaseStream.Position = oc.offset + i * 0x38 + 0x34;
                            writer.BaseStream.Position = reader.BaseStream.Position;
                            var effectHeaderOffset =
                                BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);

                            if (effectHeaderOffset != 0)
                            {
                                Console.WriteLine("Converting effect header (" + "at 0x" +
                                                  string.Format("{0:X8}", effectHeaderOffset) + ")...");
                                reader.BaseStream.Position = effectHeaderOffset;
                                writer.BaseStream.Position = reader.BaseStream.Position;

                                convertAnimationHeader(8, 1);       // flag 1 for effect type 1
                                reader.BaseStream.Position = effectHeaderOffset + 8;
                                writer.BaseStream.Position = reader.BaseStream.Position;
                                convertAnimationHeader(8, 2);       // flag 2 for effect type 2

                                // converts effect texture scroll
                                reader.BaseStream.Position = effectHeaderOffset + 16;
                                writer.BaseStream.Position = reader.BaseStream.Position;
                                reader.BaseStream.Position =
                                    BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                                writer.BaseStream.Position = reader.BaseStream.Position;

                                endianConvert(byteBuffer, 4, 2);

                                reader.BaseStream.Position = effectHeaderOffset + 20;
                                writer.BaseStream.Position = reader.BaseStream.Position;
                                endianConvert(byteBuffer, 4, 7);
                            }
                        }
                    }
                }

                void convertMys8(offsetCount oc)
                {
                    if (oc.offset != 0)
                    {
                        Console.WriteLine("Converting mystery 8s (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        //mys8test.Write("\n" + args[0] + ": " + oc.count + " foreground object(s) at 0x" + string.Format("{0:X8}", oc.offset)); 
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x38;
                            writer.BaseStream.Position = oc.offset + i * 0x38;

                            endianConvert(byteBuffer, 4, 1);            // converts the foreground object type (?)
                            reader.BaseStream.Position =
                                BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                            writer.BaseStream.Position = reader.BaseStream.Position;
                            Console.Write("\t");
                            //mys8test.Write("\n\t Object " + (i+1) + ": ");
                            byte modelNameByte;
                            do
                            {
                                modelNameByte = reader.ReadByte();      // converts model name
                                writer.Write(modelNameByte);
                                Console.Write((char)modelNameByte);
                                //mys8test.Write((char)modelNameByte);
                            } while (modelNameByte != 00);

                            Console.Write("\n");

                            reader.BaseStream.Position = oc.offset + i * 0x38 + 0x8;
                            writer.BaseStream.Position = oc.offset + i * 0x38 + 0x8;

                            endianConvert(byteBuffer, 4, 4);            // converts unknown val and x, y, z pos
                            endianConvert(byteBuffer, 2, 4);            // converts x, y, z rot and unknown val
                            endianConvert(byteBuffer, 4, 4);            // converts rest of values

                            reader.BaseStream.Position =                // converts animation header
                            BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                            writer.BaseStream.Position = reader.BaseStream.Position;

                            if (reader.BaseStream.Position != 0)
                            {
                                Console.WriteLine("Converting foreground object animation header (" + "at 0x" +
                                                  string.Format("{0:X8}", reader.BaseStream.Position) + ")...");
                                endianConvert(byteBuffer, 4, 2);        // converts unknown and anim loop point
                                convertAnimationHeader(0x58, 0);        // converts rest of animation header
                            }

                            endianConvert(byteBuffer, 4, 1);            // convert last value

                        }
                    }
                }

                void convertMys5(uint offset)
                {
                    if (offset != 0)
                    {
                        Console.WriteLine("\tConverting mystery 5s (at 0x" + string.Format("{0:X8}", offset) + ")...");
                        reader.BaseStream.Position = offset;
                        writer.BaseStream.Position = offset;
                        endianConvert(byteBuffer, 4, 5);
                    }
                }

                void convertReflective(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != reflect.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting reflective models (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0xC;
                            writer.BaseStream.Position = oc.offset + i * 0xC;


                            reader.BaseStream.Position =
                                BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                            writer.BaseStream.Position = reader.BaseStream.Position;
                            Console.Write("\t");
                            byte modelNameByte;
                            do
                            {
                                modelNameByte = reader.ReadByte();
                                writer.Write(modelNameByte);
                                Console.Write((char)modelNameByte);
                            } while (modelNameByte != 00);

                            Console.Write("\n");
                            reader.BaseStream.Position = oc.offset + i * 0x38 + 0x4;
                            writer.BaseStream.Position = oc.offset + i * 0x38 + 0x4;
                            endianConvert(byteBuffer, 4, 2);
                        }
                    }
                }

                void convertLevelModelInstance(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != levelModelInst.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting level model instances (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x24;
                            writer.BaseStream.Position = oc.offset + i * 0x24;
                        }
                    }
                }

                void convertLevelModelA(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != levelModelA.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting level models (type A) (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0xC;
                            writer.BaseStream.Position = oc.offset + i * 0xC;
                            endianConvert(byteBuffer, 4, 2);

                            reader.BaseStream.Position =
                                BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                            writer.BaseStream.Position = reader.BaseStream.Position;

                            endianConvert(byteBuffer, 4, 1);
                            reader.BaseStream.Position =
                                BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                            writer.BaseStream.Position = reader.BaseStream.Position;

                            Console.Write("\t");
                            byte modelNameByte;
                            do
                            {
                                modelNameByte = reader.ReadByte();
                                writer.Write(modelNameByte);
                                Console.Write((char)modelNameByte);
                            } while (modelNameByte != 00);

                            Console.Write("\n");

                            // Model names do not need to be converted (in terms of endianness)
                        }
                    }
                }

                void convertLevelModelB(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != levelModelB.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting level models (type B) (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x4;
                            writer.BaseStream.Position = oc.offset + i * 0x4;

                            var offsetLevelModelA =
                                BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                            convertLevelModelA(new offsetCount(1, offsetLevelModelA));
                        }
                    }
                }

                void convertSwitches(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != switches.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting switches (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x18;
                            writer.BaseStream.Position = oc.offset + i * 0x18;
                            endianConvert(byteBuffer, 4, 3);
                            endianConvert(byteBuffer, 2, 6);
                        }
                    }
                }

                void convertWormholes(offsetCount oc, bool header = false)
                {
                    if ((header || oc.offset != wormhole.offset) && oc.offset != 0)
                    {
                        if (!header) Console.Write("\t");

                        Console.WriteLine("Converting wormholes (" + oc.count + " at 0x" +
                                          string.Format("{0:X8}", oc.offset) + ")...");
                        for (var i = 0; i < oc.count; i++)
                        {
                            reader.BaseStream.Position = oc.offset + i * 0x1C;
                            writer.BaseStream.Position = oc.offset + i * 0x1C;
                            endianConvert(byteBuffer, 4, 4);
                            endianConvert(byteBuffer, 2, 4);
                            endianConvert(byteBuffer, 1, 4);
                        }
                    }
                }

                void convertAnimationHeader(uint headerLength, int animationType)
                {
                    var initialOffset = (uint)reader.BaseStream.Position;
                    Console.WriteLine("\tStarting at 0x" + string.Format("{0:X8}", initialOffset));
                    for (var offset = initialOffset; offset < initialOffset + headerLength; offset += 8)
                    {
                        Console.WriteLine("\t\tNow at 0x" + string.Format("{0:X8}", offset));
                        // sets position to current anim header offset
                        reader.BaseStream.Position = offset;
                        writer.BaseStream.Position = offset;
                        // gets the number of keyframes and converts them at the same time
                        var keyframeCount = BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                        Console.WriteLine("\t\tFound " + keyframeCount + " keyframes");
                        // goes to the offset of the keyframe list

                        if (keyframeCount != 0)
                        {
                            switch ((offset - initialOffset) / 8)
                            {
                                case 0:
                                    Console.WriteLine("\t\tType: X rotation");
                                    break;
                                case 1:
                                    Console.WriteLine("\t\tType: Y rotation");
                                    break;
                                case 2:
                                    Console.WriteLine("\t\tType: Z rotation");
                                    break;
                                case 3:
                                    Console.WriteLine("\t\tType: X translation");
                                    break;
                                case 4:
                                    Console.WriteLine("\t\tType: Y translation");
                                    break;
                                case 5:
                                    Console.WriteLine("\t\tType: Z translation");
                                    break;
                                default:
                                    Console.WriteLine("\t\tType: undefined (not a standard animation keyframe, likely BG)");
                                    break;
                            }

                            reader.BaseStream.Position =
                                BitConverter.ToUInt32(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                            writer.BaseStream.Position = reader.BaseStream.Position;
                            Console.WriteLine("\t\tTravelling to keyframe list offset 0x" +
                                              string.Format("{0:X8}", reader.BaseStream.Position));

                            // converts the keyframe list
                            for (uint currentKeyFrame = 0; currentKeyFrame < keyframeCount; currentKeyFrame++)
                            {
                                Console.WriteLine("\t\t\tConverting keyframe " + currentKeyFrame);
                                // standard keyframe
                                if (animationType == 0)
                                {
                                    //endianConvert(byteBuffer, 4, 5);
                                    Console.WriteLine("\t\t\t\tEasing type: 0x" + string.Format("{0:X8}",
                                                          BitConverter.ToUInt32(
                                                              endianConvert(byteBuffer, 4, 1, false).ToArray(), 0)));

                                    // This is code for shifting the time of all the keyframes in a level by x seconds. Useful for Bonus Hunting
                                    /*
                                    Single time = BitConverter.ToSingle(endianConvert(byteBuffer, 4, 1, false).ToArray(), 0);
                                    byte[] timeOffset = BitConverter.GetBytes(time+(float)4);
                                    List<Byte> endianTimeOffsetConv = new List<Byte>();
                                    for (int i = 0; i < timeOffset.Length; i++) endianTimeOffsetConv.Add(timeOffset[i]);
                                    endianTimeOffsetConv.Reverse();
                                    writer.BaseStream.Position -= 4;
                                    writer.Write(endianTimeOffsetConv.ToArray());

                                    writer.BaseStream.Position -= 4;
                                    reader.BaseStream.Position = writer.BaseStream.Position;*/

                                    Console.WriteLine("\t\t\t\tTime: " +
                                                      (BitConverter.ToSingle(
                                                          endianConvert(byteBuffer, 4, 1, false).ToArray(), 0) + (float)4));
                                    Console.WriteLine("\t\t\t\tValue: " +
                                                      BitConverter.ToSingle(
                                                          endianConvert(byteBuffer, 4, 1, false).ToArray(), 0));
                                    Console.WriteLine("\t\t\t\tUnknown 1: " +
                                                      BitConverter.ToSingle(
                                                          endianConvert(byteBuffer, 4, 1, false).ToArray(), 0));
                                    Console.WriteLine("\t\t\t\tUnknown 2: " +
                                                      BitConverter.ToSingle(
                                                          endianConvert(byteBuffer, 4, 1, false).ToArray(), 0));
                                }
                                // effect 1 keyframe
                                else if (animationType == 1)
                                {
                                    endianConvert(byteBuffer, 4, 3);
                                    endianConvert(byteBuffer, 2, 2);
                                    writer.Write(reader.ReadBytes(4));
                                }
                                // effect 2 keyframe
                                else if (animationType == 2)
                                {
                                    endianConvert(byteBuffer, 4, 3);
                                    endianConvert(byteBuffer, 1, 1);
                                    endianConvert(byteBuffer, 3, 1);
                                }

                                keyframeTotal++;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.SetOut(standardOutput);
                Console.WriteLine("\nConversion failed! Error details:\n" + ex +
                                  "\n\nCheck output.txt for details. Please ensure you are using an Xbox stagedef.");
            }
        }


        public struct offsetCount
        {
            public uint count;
            public uint offset;

            public offsetCount(uint count, uint offset)
            {
                this.count = count;
                this.offset = offset;
            }

            public override string ToString()
            {
                return "Count: " + count + ", Offset: 0x" + string.Format("{0:X8}", offset);
            }
        }

        public struct collisionHeader
        {
            public Vector3 centerOfRotationF;
            public Vector3 initialRotation;
            public ushort animationType;
            public uint animationHeaderOffset;
            public Vector3 conveyerF;
            public uint collisionTriangleListOffset;
            public uint collisionGridTriangleListOffset;
            public Vector2 collisionGridStartF;
            public Vector2 collisionGridStepF;
            public Vector2 collisionGridStepCount;
            public offsetCount goals;
            public offsetCount bumpers;
            public offsetCount jamabars;
            public offsetCount bananas;
            public offsetCount cone;
            public offsetCount sphere;
            public offsetCount cyl;
            public offsetCount fallout;
            public offsetCount reflective;
            public offsetCount levelModelInst;
            public offsetCount levelModelB;
            public ulong unknown9c;
            public ushort animGroupId;
            public ushort unknowna6;
            public offsetCount switches;
            public uint unknownb0;
            public uint offsetMys5;
            public float seesawSensitivityF;
            public float seesawFrictionF;
            public float seesawSpringF;
            public offsetCount wormhole;
            public uint initialAnimState;
            public uint unknownd0;
            public float animLoopPointF;
            public uint textureScrollOffset;
        }

    }
}