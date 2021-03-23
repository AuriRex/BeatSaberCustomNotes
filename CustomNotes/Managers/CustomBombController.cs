﻿using Zenject;
using UnityEngine;
using CustomNotes.Data;
using CustomNotes.Settings.Utilities;
using CustomNotes.Utilities;
using SiraUtil.Objects;

namespace CustomNotes.Managers
{
    internal class CustomBombController : MonoBehaviour, INoteControllerDidInitEvent
    {
        private PluginConfig _pluginConfig;
        private CustomNoteManager.Flags _customNoteFlags;

        private CustomNote _customNote;
        private NoteMovement _noteMovement;
        private BombNoteController _bombNoteController;
        private MeshRenderer _bombMeshRenderer;

        protected Transform bombMesh;
        protected GameObject fakeFirstPersonBombMesh;

        protected GameObject activeNote;
        protected SiraPrefabContainer container;
        protected SiraPrefabContainer.Pool bombPool;

        [Inject]
        internal void Init(PluginConfig pluginConfig, NoteAssetLoader noteAssetLoader, CustomNoteManager.Flags customNoteFlags, [InjectOptional(Id = "cn.bomb")] SiraPrefabContainer.Pool bombContainerPool)
        {
            _pluginConfig = pluginConfig;
            _customNoteFlags = customNoteFlags;

            if(_customNoteFlags.ForceDisable)
            {
                Destroy(this);
                return;
            }

            _customNote = noteAssetLoader.CustomNoteObjects[noteAssetLoader.SelectedNote];
            bombPool = bombContainerPool;

            _bombNoteController = GetComponent<BombNoteController>();
            _noteMovement = GetComponent<NoteMovement>();

            if(bombPool != null)
            {
                _bombNoteController.didInitEvent.Add(this);
                _noteMovement.noteDidFinishJumpEvent += DidFinish;
            }
            
            bombMesh = gameObject.transform.Find("Mesh");
            

            _bombMeshRenderer = GetComponentInChildren<MeshRenderer>();

            if ((_pluginConfig.HMDOnly || LayerUtils.HMDOverride))
            {
                if(bombPool == null)
                {
                    // create fake bombs for Custom Notes without Custom Bombs
                    fakeFirstPersonBombMesh = UnityEngine.Object.Instantiate(bombMesh.gameObject);
                    fakeFirstPersonBombMesh.name = "FakeFirstPersonBomb";
                    fakeFirstPersonBombMesh.transform.parent = bombMesh;

                    fakeFirstPersonBombMesh.transform.localScale = Vector3.one;
                    fakeFirstPersonBombMesh.transform.localPosition = Vector3.zero;
                    fakeFirstPersonBombMesh.transform.rotation = Quaternion.identity;
                    fakeFirstPersonBombMesh.layer = (int)LayerUtils.NoteLayer.FirstPerson;
                }
            }
            else if (bombPool != null)
            {
                _bombMeshRenderer.enabled = false;
            }
        }

        private void DidFinish()
        {
            container.transform.SetParent(null);
            bombPool.Despawn(container);
        }

        public void HandleNoteControllerDidInit(NoteController noteController)
        {
            if(_customNoteFlags.ForceDisable)
            {
                _bombMeshRenderer.enabled = true;
                Destroy(this);
                return;
            }
            SpawnThenParent(bombPool);
        }

        private void ParentNote(GameObject fakeMesh)
        {
            fakeMesh.SetActive(true);
            container.transform.SetParent(bombMesh);
            fakeMesh.transform.localPosition = container.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            container.transform.localRotation = Quaternion.identity;
            fakeMesh.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f) * Utils.NoteSizeFromConfig(_pluginConfig);
            container.transform.localScale = Vector3.one;
        }

        private void SpawnThenParent(SiraPrefabContainer.Pool bombModelPool)
        {
            container = bombModelPool.Spawn();
            activeNote = container.Prefab;
            bombPool = bombModelPool;
            if (_pluginConfig.HMDOnly == true || LayerUtils.HMDOverride == true)
            {
                LayerUtils.SetLayer(activeNote, LayerUtils.NoteLayer.FirstPerson);
            }
            else
            {
                LayerUtils.SetLayer(activeNote, LayerUtils.NoteLayer.Note);
            }
            ParentNote(activeNote);
        }

        protected void OnDestroy()
        {
            if (_bombNoteController != null)
            {
                _bombNoteController.didInitEvent.Remove(this);
            }
            Destroy(fakeFirstPersonBombMesh);
        }
    }
}
