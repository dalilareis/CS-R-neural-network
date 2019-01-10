using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeatureExtractor
{
    class MouseEvents
    {
        private string id;
        private string type;
        private long timestamp;
        private int x;
        private int y;

        public MouseEvents(string id, string type, long timestamp, int x, int y)
        {
            this.id = id;
            this.type = type;
            this.timestamp = timestamp;
            this.x = x;
            this.y = y;
        }
        public string getID()
        {
            return this.id;
        }
        public string getType()
        {
            return this.type;
        }
        public long getTimestamp()
        {
            return this.timestamp;
        }
        public int getX()
        {
            return this.x;
        }
        public int getY()
        {
            return this.y;
        }

        public void setID(string id)
        {
            this.id = id;
        }
        public void setType(string type)
        {
            this.type = type;
        }
        public void setTimestamp(int timestamp)
        {
            this.timestamp = timestamp;
        }
        public void setX(int x)
        {
            this.x = x;
        }
        public void setY(int y)
        {
            this.y = y;
        }
        

    }
}
