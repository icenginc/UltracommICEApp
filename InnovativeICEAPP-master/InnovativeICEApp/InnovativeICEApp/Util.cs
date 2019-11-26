/**
 * Copyright (c) 2015 Ultra Communications Inc.  All rights reserved.
 * 
 * $Id: Util.cs 369 2015-04-01 23:07:01Z vahid $
 **/

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using UltraCommunications.Service;

namespace InnovativeICEApp
{
    class Util
    {
        /// <summary>
        /// This function exists to isolate the FormConfig class from ServiceQuery to allow EmberMon
        /// configurations that run without remote support.
        /// </summary>
        /// <param name="cbx">Checkbox to fill-in</param>
        /// <param name="items">Dictionary receiving remote information</param>
        /// <param name="service_class">service class to search for</param>
        /// <param name="identity">identity to search for (if any)</param>
        /// <param name="port">port # to use</param>
        public static void Query(ComboBox cbx, Dictionary<string, string> items, string service_class, string identity, int port)
        {
			ServiceQuery sq = new ServiceQuery(service_class, identity, port);
			// populate power meter selection (non-server)
			if (sq.INFO.Length > 0)
            {
                foreach (Info info in sq.INFO)
                {
                    string id = (info.hostname + ":" + info.identity).ToUpper();
                    if (cbx != null)
                    {
                        cbx.Items.Add(id);
                    }
                    if (!items.ContainsKey(id))
                    {
                        items.Add(id, info.identity + ":" + info.endpoint);
                    }
                }
            }
        }
    }
}
