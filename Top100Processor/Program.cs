using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dapper;
using MySql.Data.MySqlClient;

namespace Top100Processor
{
    class Program
    {
        static void Main(string[] args)
        {
            ScoreData[] originalData;
            ScoreData[] alteredData;

            using (MySqlConnection conn = new MySqlConnection(AppSettings.ConnectionStringOriginal))
                originalData = queryData(conn);
            using (MySqlConnection conn = new MySqlConnection(AppSettings.ConnectionStringAltered))
                alteredData = queryData(conn);

            var top = originalData.Take(100).Union(alteredData.Take(100), EqualityComparer<ScoreData>.Default).OrderBy(d => d.pp).Reverse().ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("beatmap,username,rank,mods,original pp,new pp,diff");

            foreach (var score in top)
            {
                var origScore = originalData.Single(s => s.score_id == score.score_id);
                var altScore = alteredData.Single(s => s.score_id == score.score_id);

                var mods = ((Mods)(origScore.enabled_mods & ~512)).ToString();

                sb.AppendLine(
                    $"\"{origScore.filename.Replace(".osu", "")}\",{origScore.username},{origScore.rank},\"{mods}\",{origScore.pp},{altScore.pp},{altScore.pp - origScore.pp}");
            }

            File.WriteAllText("out.csv", sb.ToString());
        }

        private static ScoreData[] queryData(MySqlConnection conn) => conn.Query<ScoreData>(@"SELECT * FROM (
                                                SELECT s.score_id,b.filename,u.username,s.enabled_mods,s.rank,s.pp
                                                FROM osu_scores_high AS s
                                                JOIN osu_beatmaps b ON b.beatmap_id = s.beatmap_id
                                                JOIN sample_users u ON u.user_id = s.user_id
                                                WHERE (s.enabled_mods & 1024) > 0 ORDER BY pp DESC LIMIT 300
                                                ) as D;").ToArray();

        public class ScoreData : IEquatable<ScoreData>
        {
            public uint score_id { get; set; }

            public string filename { get; set; }

            public int enabled_mods { get; set; }

            public string rank { get; set; }

            public double pp { get; set; }

            public string username { get; set; }

            public bool Equals(ScoreData other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return score_id == other.score_id;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ScoreData) obj);
            }

            public override int GetHashCode()
            {
                return (int) score_id;
            }
        }

        [Flags]
        public enum Mods
        {
            None = 0,
            NF = 1,
            EZ = 2,
            HD = 8,
            HR = 16,
            SD = 32,
            DT = 64,
            HT = 256,
            FL = 1024,
        }
    }
}