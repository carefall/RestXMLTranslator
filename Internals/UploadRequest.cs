using System;
using System.Collections.Generic;
using System.Text;

namespace RestXMLTranslator.Internals
{
    public class UploadRequest
    {
        public List<UploadEntry> Entries { get; set; } = [];
    }
}
