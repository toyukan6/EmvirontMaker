using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnvironmentMaker {
    class MyTime {
        public int Hour { get; private set; }
        public int Minute { get; private set; }
        public int Second { get; private set; }
        public int MilliSecond { get; private set; }
        public MyTime(int hour, int minute, int second, int milli) {
            Hour = hour;
            Minute = minute;
            Second = second;
            MilliSecond = milli;
        }

        public int GetMilli() {
            return Second * 1000 + MilliSecond;
        }
    }
}
