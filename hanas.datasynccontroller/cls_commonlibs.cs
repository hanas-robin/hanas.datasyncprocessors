using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using hanas.com.covar;


namespace hanas.datasynccontroller
{
    class cls_commonlibs
    {
        public static cls_covar00 c_commonvars;

        public string host_type { set; get; }
        public string host_localname { set; get; }
        public string host_localcode { set; get; }
        public string host_remotename { set; get; }
        public string host_remotecode { set; get; }
        public string host_localipaddress { set; get; }
        public string host_remoteipaddress { set; get; }

        public cls_commonlibs()
        {
            c_commonvars = new cls_covar00();
        }
    }
}
