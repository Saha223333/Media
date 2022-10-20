using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewProject
{//здесь будем хранить значения полей с главной формы для записи в устройства
    public class FieldsValuesToWrite
    {
        public string name;
        public string value;

        public FieldsValuesToWrite(string pname, string pvalue)
        {
            this.name = pname;
            this.value = pvalue;
        }
    }
}
