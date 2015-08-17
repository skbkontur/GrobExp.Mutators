﻿using System.Collections.Generic;
using System.Linq;

namespace GrobExp.Mutators.AssignRecording
{
    public class AssignRecordCollection
    {
        public AssignRecordCollection()
        {
            converterRecords = new List<RecordNode>();
        }

        public void AddConverterToRecord(string converterName)
        {
            if(converterRecords.Select(converter => converter.Name).Contains(converterName))
                currentConverterRecord = GetConverterByName(converterName);
            else
            {
                currentConverterRecord = new RecordNode(converterName);
                converterRecords.Add(currentConverterRecord);
            }
        }

        private RecordNode GetConverterByName(string converterName)
        {
            return converterRecords.FirstOrDefault(converter => converter.Name == converterName);
        }

        public void AddRecord(string path, string value)
        {
            currentConverterRecord.AddRecord(path, value);
        }

        public void MarkExecutedRecord(string path, string value)
        {
            currentConverterRecord.MarkExecutedRecord(path, value);
        }

        public List<RecordNode> GetRecords()
        {
            return converterRecords;
        }

        private readonly List<RecordNode> converterRecords;
        private RecordNode currentConverterRecord;
    }
}