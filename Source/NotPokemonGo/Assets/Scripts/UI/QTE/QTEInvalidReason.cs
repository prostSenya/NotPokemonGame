using System;

namespace UI.QTE
{
  [Serializable]
  public enum QTEInvalidReason
  {
    Unknown = 0,
    Timeout = 1,
    OutOfBounds = 2,
    WrongInput = 3,
  }
}
