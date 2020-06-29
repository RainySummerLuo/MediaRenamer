﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaInfoLib;
using MetadataExtractor.Formats.Exif;
using static System.TimeZoneInfo;
using static MetadataExtractor.ImageMetadataReader;

namespace MediaRenamer {
    public static class MetadataQuery {
        public static Dictionary<string, string> MetaQuery(FileSystemInfo file, bool filetype) {
            try {
                Dictionary<string, string> dictResult;
                Dictionary<string, string> dictDatetime;

                switch (filetype) {
                    case true:
                        // file type is image
                        dictResult = new Dictionary<string, string> {{"type", "Pic"}};
                        var directories = ReadMetadata(file.FullName);
                        dictDatetime = PicDtQuery(directories);
                        if (dictDatetime != null) {
                            foreach (var dt in dictDatetime) {
                                dictResult.Add(dt.Key, dt.Value);
                            }
                        } else {
                            return null;
                        }
                        return dictResult;
                    case false:
                        // file type is video
                        dictResult = new Dictionary<string, string> {{"type", "Vid"}};
                        dictDatetime = VidDtQuery(file);
                        if (dictDatetime != null) {
                            foreach (var dt in dictDatetime) {
                                dictResult.Add(dt.Key, dt.Value);
                            }
                        } else {
                            return null;
                        }
                        return dictResult;
                    default:
                        return null;
                }
            } catch (Exception e) {
                Console.WriteLine(e);
                return null;
            }
        }

        private static Dictionary<string, string> PicDtQuery(IReadOnlyList<MetadataExtractor.Directory> directories) {
            try {
                var subdirDt = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                var strDt = subdirDt?.GetDescription(ExifDirectoryBase.TagDateTime);
                const string strDtFormat = "yyyy.MM.dd_HHmmss";
                var dtDt = DateTime.ParseExact(strDt, "yyyy:MM:dd HH:mm:ss", CultureInfo.CurrentCulture);
                var startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));
                var timestamp = (dtDt.Ticks - startTime.Ticks) / 10000;
                var dictDt = new Dictionary<string, string> {
                    {"datetime", dtDt.ToString(strDtFormat)},
                    {"timestamp", timestamp + ""}
                };
                return dictDt;
            } catch (Exception) {
                return null;
            }
        }

        private static Dictionary<string, string> VidDtQuery(FileSystemInfo file) {
            string strDt;
            var mi = new MediaInfo();
            mi.Open(file.FullName);
            try {
                strDt = mi.Get(StreamKind.Video, 0, "Encoded_Date");
                if (string.IsNullOrEmpty(strDt)) {
                    strDt = mi.Get(StreamKind.Video, 0, "Tagged_Date");
                }

                if (string.IsNullOrEmpty(strDt)) {
                    strDt = mi.Get(StreamKind.General, 0, "Recorded_Date");
                }

                mi.Close();
            } catch (Exception) {
                mi.Close();
                return null;
            }
            mi.Dispose();
            const string strDtFormat = "yyyy.MM.dd_HHmmss";
            var strTz = strDt.Substring(0, 3);
            strDt = strDt.Replace(strTz + " ", "");
            var dtDt = DateTime.ParseExact(strDt, "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
            dtDt = ConvertTimeFromUtc(dtDt, Local);
            var startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));
            var timestamp = (dtDt.Ticks - startTime.Ticks) / 10000;
            var dictDt = new Dictionary<string, string> {
                {"datetime", dtDt.ToString(strDtFormat)},
                {"timestamp", timestamp + ""}
            };
            return dictDt;
        }
    }
}