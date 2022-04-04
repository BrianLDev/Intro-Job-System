using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour {

  public void OnMove(InputAction.CallbackContext context) {
    Debug.Log(context);
  }

}


