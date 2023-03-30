
using System;

namespace UniversaLIS
{
     partial class UniversaLIService : IDisposable
     {
          /// <summary> 
          /// Required designer variable.
          /// </summary>
          private System.ComponentModel.IContainer components = null;

          /// <summary>
          /// Clean up any resources being used.
          /// </summary>
          /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
          public override void Dispose()
          {
               if (components != null)
               {
                    components.Dispose();
               }
               base.Dispose();
          }

     }
}
