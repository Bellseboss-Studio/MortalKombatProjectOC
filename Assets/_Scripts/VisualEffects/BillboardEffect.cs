using System;
using Unity.Cinemachine;
using UnityEngine;


public class BillboardEffect : MonoBehaviour
{
    private Camera m_MainCamera;
    [SerializeField] private CinemachineBrain m_CinemachineBrain;
    private void Awake()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        m_MainCamera = Camera.main;
       
        if (!m_MainCamera)
        {
            Debug.LogError("Main camera not found");
            return;
        }

        m_CinemachineBrain = m_MainCamera.GetComponent<CinemachineBrain>();
        
        if (!m_CinemachineBrain)
        {
            Debug.LogError("CinemachineBrain not found on the main camera.");
            return;
        }

    }

    private void Update()
    {
        if (!m_CinemachineBrain)
        {
            return;
        }
        //TODO Change references to CinemachineVirtualCamera as it is obsolete
        var activeVirtualCamera = m_CinemachineBrain.ActiveVirtualCamera as CinemachineVirtualCamera;

        if (!activeVirtualCamera)
        {
            return;
        }

        CinemachineVirtualCamera virtualCamera = activeVirtualCamera;
        
        if (virtualCamera && virtualCamera.LookAt)
        {
            transform.LookAt(virtualCamera.LookAt.position, transform.forward);
        }
        else
        {
            transform.LookAt(activeVirtualCamera.transform.position, transform.up);
        }
    }
    
   
    
}
