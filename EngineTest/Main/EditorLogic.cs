﻿using System.Collections.Generic;
using DeferredEngine.Entities;
using DeferredEngine.Recources;
using DeferredEngine.Renderer.Helper;
using HelperSuite.GUIHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DeferredEngine.Main
{
    public class EditorLogic
    {
        //private int _selectedId = 0;
        
        private bool _gizmoTransformationMode;
        private Vector3 _gizmoPosition;
        private int _gizmoId;
        private GizmoModes _gizmoMode = GizmoModes.Translation;

        public TransformableObject SelectedObject;

        private GraphicsDevice _graphicsDevice;

        public enum GizmoModes
        {
            Translation,
            Rotation
        }

        public struct EditorReceivedData
        {
           public int HoveredId;
            public Matrix ViewMatrix;
            public Matrix ProjectionMatrix;
        }

        public struct EditorSendData
        {
            public TransformableObject SelectedObject;
            public int SelectedObjectId;
            public Vector3 SelectedObjectPosition;
            public bool GizmoTransformationMode;
            public GizmoModes GizmoMode;
        }

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
        }

        /// <summary>
        /// Main Logic for the editor part
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="entities"></param>
        /// <param name="data"></param>
        public void Update(GameTime gameTime, List<BasicEntity> entities, List<PointLightSource> pointLights, List<DirectionalLightSource> dirLights, EnvironmentSample envSample, EditorReceivedData data, MeshMaterialLibrary meshMaterialLibrary)
        {
            if (!GameSettings.Editor_enable) return;

            if(Input.WasKeyPressed(Keys.R)) _gizmoMode = GizmoModes.Rotation;
            if (Input.WasKeyPressed(Keys.T)) _gizmoMode = GizmoModes.Translation;

            int hoveredId = data.HoveredId;

            if (_gizmoTransformationMode)
            {
                if (Input.mouseState.LeftButton == ButtonState.Pressed)
                {
                    GizmoControl(_gizmoId, data);
                }
                else _gizmoTransformationMode = false;
            }
            else if (Input.WasLMBClicked() && !GUIControl.UIWasUsed)
            {
                //Gizmos
                if (hoveredId >= 1 && hoveredId <= 3)
                {
                    _gizmoId = hoveredId;
                    GizmoControl(_gizmoId, data);
                    return;
                }

                if (hoveredId <= 0)
                {
                    SelectedObject = null;
                    return;
                }

                bool foundnew = false;
                //Get the selected entity!
                for (int index = 0; index < entities.Count; index++)
                {
                    var VARIABLE = entities[index];
                    if (VARIABLE.Id == hoveredId)
                    {
                        SelectedObject = VARIABLE;
                        foundnew = true;
                        break;
                    }
                }
                if (foundnew == false)
                {
                    for (int index = 0; index < pointLights.Count; index++)
                    {
                        PointLightSource pointLightSource = pointLights[index];
                        if (pointLightSource.Id == hoveredId)
                        {
                            SelectedObject = pointLightSource;
                            break;
                        }
                    }

                    for (int index = 0; index < dirLights.Count; index++)
                    {
                        DirectionalLightSource directionalLightSource = dirLights[index];
                        if (directionalLightSource.Id == hoveredId)
                        {
                            SelectedObject = directionalLightSource;
                            break;
                        }
                    }

                    {
                        if (envSample.Id == hoveredId)
                        {
                            SelectedObject = envSample;
                        }
                    }
                }

            }

            //Controls

            if (Input.WasKeyPressed(Keys.Delete))
            {
                    //Find object
                    if (SelectedObject is BasicEntity)
                    {
                        entities.Remove((BasicEntity) SelectedObject);
                        meshMaterialLibrary.DeleteFromRegistry((BasicEntity) SelectedObject);

                        SelectedObject = null;
                    }

                    else if (SelectedObject is PointLightSource)
                    {
                        pointLights.Remove((PointLightSource)SelectedObject);
                        
                        SelectedObject = null;
                    }
                    else if (SelectedObject is DirectionalLightSource)
                    {
                        dirLights.Remove((DirectionalLightSource)SelectedObject);

                        SelectedObject = null;
                    }
            }

            if (Input.WasKeyPressed(Keys.Insert) || (Input.keyboardState.IsKeyDown(Keys.LeftControl) && Input.WasKeyPressed(Keys.C)))
            {
                if (SelectedObject is BasicEntity)
                {
                    BasicEntity copy = (BasicEntity)SelectedObject.Clone;
                    copy.RegisterInLibrary(meshMaterialLibrary);
    
                    entities.Add(copy);
                }
                else if (SelectedObject is PointLightSource)
                {
                    PointLightSource copy = (PointLightSource)SelectedObject.Clone;
                    pointLights.Add(copy);
                }
                else if (SelectedObject is DirectionalLightSource)
                {
                    DirectionalLightSource copy = (DirectionalLightSource)SelectedObject.Clone;
                    dirLights.Add(copy);
                }
            }
            
        }

        private void GizmoControl(int gizmoId, EditorReceivedData data)
        {
            //there must be a selected object for a gizmo

            float x = Input.mouseState.X;
            float y = Input.mouseState.Y;

            Vector3 pos1 =
                _graphicsDevice.Viewport.Unproject(new Vector3(x, y, 0),
                    data.ProjectionMatrix, data.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = _graphicsDevice.Viewport.Unproject(new Vector3(x, y, 1),
                    data.ProjectionMatrix, data.ViewMatrix, Matrix.Identity);

            Ray ray = new Ray(pos1, pos2-pos1);

            Plane plane = new Plane();

            if (_gizmoMode == GizmoModes.Translation)
            {
                if (gizmoId == 1)
                {
                    plane = new Plane(SelectedObject.Position, SelectedObject.Position + Vector3.UnitZ,
                        SelectedObject.Position + Vector3.UnitY);
                }
                else if (gizmoId == 2)
                {
                    plane = new Plane(SelectedObject.Position, SelectedObject.Position + Vector3.UnitY,
                        SelectedObject.Position + Vector3.UnitZ);
                }
                else if (gizmoId == 3)
                {
                    plane = new Plane(SelectedObject.Position, SelectedObject.Position + Vector3.UnitZ,
                        SelectedObject.Position + Vector3.UnitX);
                }
            }
            else //rotation
            {
                //Z is up
                Vector3 forwardVector3 = pos2 - pos1;
                forwardVector3.Normalize();

                //left
                Vector3 leftVector3 = Vector3.Cross(Vector3.UnitZ, forwardVector3);

                //UP
                Vector3 upVector3 = Vector3.Cross(forwardVector3, leftVector3);

                //alternative
                plane = new Plane(SelectedObject.Position, SelectedObject.Position + upVector3,
                        SelectedObject.Position + leftVector3);
            }

            float? d = ray.Intersects(plane);

            if (d == null) return;

            float f = (float) d;

            Vector3 hitPoint = pos1 + (pos2 - pos1)*f;

            if (_gizmoTransformationMode == false)
            {
                _gizmoTransformationMode = true;
                _gizmoPosition = hitPoint;
                return;
            }
            
            //Get the difference
            Vector3 diff = hitPoint - _gizmoPosition;

            if (_gizmoMode == GizmoModes.Translation)
            {
                diff.Z *= gizmoId == 1 ? 1 : 0;
                diff.Y *= gizmoId == 2 ? 1 : 0;
                diff.X *= gizmoId == 3 ? 1 : 0;

                SelectedObject.Position += diff;
            }
            else
            {
                diff.Z *= gizmoId == 1 ? 0 : 1;
                diff.Y *= gizmoId == 2 ? 0 : 1;
                diff.X *= gizmoId == 3 ? 0 : 1;

                float diffL = diff.X + diff.Y + diff.Z;

                diffL /= 10;

                //if (gizmoId == 1) //Z
                //{
                //    SelectedObject.AngleZ += diffL;
                //}
                //if (gizmoId == 2) //Z
                //{
                //    SelectedObject.AngleY += diffL;
                //}
                //if (gizmoId == 3) //Z
                //{
                //    SelectedObject.AngleX += diffL;
                //}
                if (gizmoId == 1)
                {
                    SelectedObject.RotationMatrix = SelectedObject.RotationMatrix * Matrix.CreateRotationZ((float)diffL);
                }
                if (gizmoId == 2)
                {
                    SelectedObject.RotationMatrix = SelectedObject.RotationMatrix * Matrix.CreateRotationY((float)diffL);
                }
                if (gizmoId == 3)
                {
                    SelectedObject.RotationMatrix = SelectedObject.RotationMatrix * Matrix.CreateRotationX((float)diffL);
                }
            }


            _gizmoPosition = hitPoint;

        }

        public EditorSendData GetEditorData()
        {
            if (SelectedObject == null)
                return new EditorSendData {SelectedObjectId = 0, SelectedObjectPosition = Vector3.Zero};
            return new EditorSendData
            {
                SelectedObject = SelectedObject,
                SelectedObjectId = SelectedObject.Id,
                SelectedObjectPosition = SelectedObject.Position,
                GizmoTransformationMode = _gizmoTransformationMode,
                GizmoMode =  _gizmoMode,
                
            };
        }

    }
}
