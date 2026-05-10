using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTG_Tools.ClassesStructs
{
    public class PropertyClass
    {
        public class ClassPropertySet
        {
            struct inengineValueTypes
            {
                public string name;
                public byte[] crc64_code;
            }

            public struct props
            {
                public importProps imProps;
                public int block_length;
                public int count;
                public prop_values[] vals;
            }

            public struct im_props
            {
                public int str_name_len;
                public string str_name;
            }

            public struct values
            {
                public byte[] crc64_val_type;
                public int block_len_str_type;
                public int str_type_len;
                public string str_type;

                public string str_val;
            }

            public struct prop_values
            {
                public byte[] crc64_value;
                public int str_val_len;
                public string str_val; // value type

                public int count_values;
                public values[] values_s;
            }

            public struct importProps
            {
                public int block_length;
                public int count;
                public im_props[] import_props;
            }

            public props properties;
        }
    }
}
