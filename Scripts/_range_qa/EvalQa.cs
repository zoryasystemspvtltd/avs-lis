using System;
using System.Collections.Generic;
using LIS.BusinessLogic;
using LIS.DtoModel.Models;

class E {
  static int Main() {
    var param = new HISParameterMaster { Id = 1, HISParamCode = "X" };
    var patient = new PatientDetail { Age = 30, Gender = "Male" };
    var ranges = new List<HISParameterRangMaster> {
      new HISParameterRangMaster { Id = 1, HisParameterId = 1, Gender = "Both", AgeFrom = 0, AgeTo = 99, MinValue = 10, MaxValue = 20, HISRangeValue = "DISPLAY-RANGE-VALUE" }
    };
    string rr; string flag; bool abn;
    TestResultRangeEvaluator.Apply("15", param, patient, ranges, out rr, out flag, out abn);
    Console.WriteLine("EVAL_REF=" + rr);
    if (rr != "DISPLAY-RANGE-VALUE") { Console.WriteLine("FAIL prefer HISRangeValue"); return 5; }
    Console.WriteLine("PASS_EVALUATOR");
    return 0;
  }
}
