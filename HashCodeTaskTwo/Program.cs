using System;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace HashCodeTaskTwo
{
    class Program
    {
        static void Main(string[] args)
        {
            string fname = Console.ReadLine();
            Task t = Task.LoadFromFile(fname+".txt");
            Algo.t = t;
            Algo.Start();
            Stopwatch timer = new Stopwatch(); timer.Start();
            Console.WriteLine("result: " + Algo.success);
            File.AppendAllText("history", fname+" : "+Algo.success+"\n----"+timer.Elapsed+"-------\n");
            Extractor.Extract(fname+".out");
            Console.ReadLine();
        }
    }



    class Algo
    {
        static bool Registering = false;
        static HashSet<int> Used = new HashSet<int>();
        static HashSet<int> AvaliableIds = new HashSet<int>();
        public static long success = 0;

        static public Task t;

        static private void CheckForRegister()
        {
            for (int i = 0; i < t.libs.Count; i++)
            {
                if (!t.libs[i].registered && !Registering)
                {
                    Register(t.libs[i]);
                    break;
                }
            }
        }
        static private void Register(Library l)
        {
            Registering = true;
            l.registering = true;
        }
        static private int[] FindIDOfMax(Library l, int[] arr)
        {
            int[] result = { -1, -1 };
            int Max = int.MinValue;
            for(int i = 0; i < arr.Length; i++)
            {
                if (arr[i] > Max && !Used.Contains(l.bookids[i]) 
                    )    
                { 
                    result[0] = arr[i]; result[1] = i; 
                }
                else if (Used.Contains(l.bookids[i]))
                {
                    l.bookids.RemoveAt(i); l.booksScores.RemoveAt(i);
                }
            } 
              return result;
        }
        static private void SendBooks(Library l, Days.Day day)
        {
            for (int i = 0; i < l.shipment; i++)
            {
                if (l.bookids.Count == 0) return;
                int[] res = FindIDOfMax(l, l.booksScores.ToArray());
                int rem = res[1];
                res[1] = l.bookids[res[1]];
                day.acts.Add(new Action(false, l.libid, res[1]));
                l.bookids.RemoveAt(rem);
                l.booksScores.RemoveAt(rem);
                l.awarded += res[0];
                success += res[0];
            }
        }
        static private void Optimizer(int DaysLeft)
        {
            foreach (Library lib in t.libs)
            {
                foreach (int id in Used)
                {
                    if (lib.bookids.Contains(id))
                    {
                        int indx = lib.bookids.FindIndex(x => x == id);
                        lib.bookids.Remove(id);
                        lib.booksScores.RemoveAt(indx);
                    }
                }
                lib.RecountEff(DaysLeft);
            }
            t.libs.Sort(
                delegate (Library pair1, Library pair2)
                {
                    if (pair1.eff > pair2.eff) return 1; else if (pair1.eff < pair2.eff) return -1; else return 0;
                });
        }
        static void UpdateAvaliables(Library l)
        {
            foreach (int id in l.bookids) 
            { 
                AvaliableIds.Add(id);
                foreach (Library lib in t.libs)
                {
                    if (!lib.registered && lib.libid != l.libid)
                    {
                        int indx = lib.bookids.FindIndex(x => x == id);
                        if (indx > 0)
                        {
                            lib.bookids.Remove(id);
                            lib.booksScores.RemoveAt(indx);
                        }
                    } else continue;
                }
            }
        }
        static public void Start()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int day = 0; day < Task.daystoscan; day++)
            {
                Days.dayz.Add(new Days.Day());
                CheckForRegister();
                Console.WriteLine("Day : " + (day + 1) + " / " + Task.daystoscan+" ETA: "+((sw.ElapsedMilliseconds/(day+1))*Task.daystoscan/1000/60)+" minutes");

                for (int lib = 0; lib< t.libs.Count; lib++)
                {
                    //Console.WriteLine(S + "\nLib #"+lib+" / "+t.libs.Count);
                    if (t.libs[lib].registering && t.libs[lib].signIn > 0 && Registering
                        && !t.libs[lib].registered)
                    {
                        Days.dayz[day].acts.Add(new Action(true, t.libs[lib].libid, -1));
                        t.libs[lib].signIn--;
                    }
                    else if (t.libs[lib].registering && t.libs[lib].signIn <= 0
                        && !t.libs[lib].registered)
                    {
                        t.libs[lib].registering = false;
                        Registering = false;
                        t.libs[lib].registered = true;
                        UpdateAvaliables(t.libs[lib]);
                        SendBooks(t.libs[lib], Days.dayz[day]);
                    }
                    else if (t.libs[lib].registered)
                    {
                        if (t.libs[lib].bookids.Count > 0)
                            SendBooks(t.libs[lib], Days.dayz[day]);
                        else
                        {
                            t.libs.RemoveAt(lib);
                        }
                    }


                    Optimizer(Task.daystoscan-day);

                }






            }
        }
    }




    class Days
    {
        public class Day
        {
            public List<Action> acts = new List<Action>();
        }
        public static List<Day> dayz = new List<Day>();
    }
    class Action
    {
        public bool sign =false; 
        public int lib =  -1;
        public int book = -1;

        public Action(bool sign, int lib, int book) { this.sign = sign; this.lib = lib; this.book = book; }
    }


    class Extractor
    {
        static public void Extract(string name)
        {
            string result = "";
            List<Action> allacts = new List<Action>();
            for(int i =0; i < Days.dayz.Count; i++)
            {
                allacts.AddRange(Days.dayz[i].acts);
            }
            int lockedid = -1;
            List<Action> actlib = new List<Action>();
            int regs = 0;
            while (allacts.Count > 0)
            {
                if (lockedid == -1)
                {
                    for (int i = 0; i < allacts.Count; i++)
                    {
                        if (allacts[i].sign)
                        {
                            lockedid = allacts[i].lib;
                            result += lockedid; result += ' ';
                            actlib = allacts.Where(x => x.lib == lockedid && x.sign == false).ToList();
                            result += actlib.Count; result += '\n';
                            allacts.RemoveAll(x => x.lib == lockedid);
                            regs++;
                            break;
                        }
                    }
                }
                Dictionary<int, int> oper = new Dictionary<int, int>(Algo.t.books);
                List<int> ids = oper.Keys.ToList();
                List<int> scores = oper.Values.ToList();
                for (int i = 0; i < actlib.Count; i++)
                {

                    result += actlib[i].book; if (i+1 != actlib.Count) result += ' ';                    
                }
                lockedid = -1;
                result += '\n';
            }
            string fullres = ""; fullres += regs; fullres += '\n'; fullres += result;
            fullres = fullres.Remove(fullres.Length - 1);
            File.WriteAllText(name, fullres);
        }

    }


    class Task
    {
        public int libsN;
        public int booksN;
        public static int daystoscan;
        public Dictionary<int, int> books = new Dictionary<int, int>();
        public List<Library> libs = new List<Library>();
        public List<KeyValuePair<int, int>> cheats = new List<KeyValuePair<int, int>>();

        public static Task LoadFromFile(string name)
        {
            Task result = new Task();
            string txt = File.ReadAllText(name);
            string[] lines = txt.Split('\n');
            string line = lines[0];
            string[] vals = line.Split(' ');
            result.booksN = int.Parse(vals[0]);
            result.libsN = int.Parse(vals[1]);
            daystoscan = int.Parse(vals[2]);

            line = lines[1];
            vals = line.Split(' ');
            for(int i = 0; i < vals.Length; i++)
            {
                result.books.Add(i, int.Parse(vals[i]));
            }
            result.cheats = result.books.ToList();
            result.cheats.Sort(
                delegate (KeyValuePair<int, int> pair1,
                    KeyValuePair<int, int> pair2)
                    {
                        return pair1.Value.CompareTo(pair2.Value);
                    });

            int linecounter = 2;
            for (int i = 0; i < result.libsN; i++)
            {
                Library lib = new Library();
                lib.libid = i;
                line = lines[i+linecounter];
                vals = line.Split(' ');
                lib.booksN = int.Parse(vals[0]);
                lib.signIn = int.Parse(vals[1]);
                lib.startsigin = lib.signIn;
                lib.shipment = int.Parse(vals[2]);
                line = lines[i + (++linecounter)];
                vals = line.Split(' ');
                lib.booksN = vals.Length;
                List<int> Scores = new List<int>();
                for (int j = 0; j < lib.booksN; j++)
                {
                    result.books.TryGetValue(int.Parse(vals[j]), out int score);
                    Scores.Add(score);
                    lib.booksScores.Add(score);
                    lib.bookids.Add(int.Parse(vals[j]));
                    lib.libraryscore += score;
                }
                //Scores.Sort();
                //lib.booksScores = Scores.ToArray();
                lib.eff = (double)lib.libraryscore / (double)lib.booksN * ((double)lib.booksN/(double)lib.shipment);
                lib.effdays = (double)daystoscan - (double)lib.signIn;
                lib.eff *= (double)lib.effdays;
                result.libs.Add(lib);
            }
            result.libs.Sort(
                delegate (Library pair1, Library pair2)
            {
                if (pair1.eff > pair2.eff) return 1; else if (pair1.eff < pair2.eff) return -1; else return 0;
            });

            return result;
        }
    }
    class Library
    {
        public bool registered = false;
        public bool registering = false;

        public int libid;
        public int bookpointer = 0;
        public int booksN;
        public int startsigin;
        public int signIn;
        public int shipment;
        public List<int> booksScores = new List<int>();
        public List<int> bookids = new List<int>();
        public int libraryscore = 0;
        public double eff = 0;
        public double effdays = 0;

        public int awarded = 0;

        public void RecountEff(int DaysLeft)
        {
            this.libraryscore = 0;
            foreach (int score in this.booksScores) libraryscore += score;
            this.eff = (double)this.libraryscore / (double)this.booksScores.Count;
            this.eff = ((double)this.booksScores.Count / (double)this.shipment);
            this.effdays = (double)DaysLeft - (double)this.signIn;
            this.eff *= (double)this.effdays;
        }
    }
}
