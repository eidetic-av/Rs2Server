using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Eidetic.Rs2
{
    public partial class Rs2Server : MonoBehaviour
    {
        void InitialiseUI()
        {
            for (int i = 0; i < Drivers.Count(); i++)
            {
                var cam = Cameras[i];
                var previewImage = GameObject.Find($"ColorTexture{i}")
                    .GetComponent<RawImage>();
                var enableButton = GameObject.Find($"EnableButton{i}")
                    .GetComponent<Button>();
                enableButton.onClick.AddListener(() =>
                {
                    cam.Active = !cam.Active;
                    var enableLabel = enableButton.gameObject
                        .GetComponentsInChildren<Text>().First();
                    enableLabel.text = cam.Active ? "Disable" : "Enable";
                    if (!cam.Active) previewImage.texture = null;
                });
                var pauseButton = GameObject.Find($"PauseButton{i}")
                    .GetComponent<Button>();
                pauseButton.onClick.AddListener(() =>
                {
                    cam.Paused = !cam.Paused;
                    var pauseLabel = pauseButton.gameObject
                        .GetComponentsInChildren<Text>().First();
                    pauseLabel.text = cam.Paused ? "Resume" : "Pause";
                });
                var brightnessSlider = GameObject.Find($"BrightnessSlider{i}")
                    .GetComponent<Slider>();
                brightnessSlider.onValueChanged.AddListener((newVal) =>
                                                            cam.Brightness = newVal );
                var saturationSlider = GameObject.Find($"SaturationSlider{i}")
                    .GetComponent<Slider>();
                saturationSlider.onValueChanged.AddListener((newVal) =>
                                                            cam.Saturation = newVal );

                var pointThresholdSliderXMin = GameObject.Find($"PointThresholdXMin{i}")
                    .GetComponent<Slider>();
                var pointThresholdSliderXMax = GameObject.Find($"PointThresholdXMax{i}")
                    .GetComponent<Slider>();
                pointThresholdSliderXMin.onValueChanged.AddListener((newVal) =>
                {
                    var x = newVal;
                    var y = cam.PointThreshold.Min.y;
                    var z = cam.PointThreshold.Min.z;
                    cam.PointThreshold.Min = new Vector3(x, y, z);
                });
                pointThresholdSliderXMax.onValueChanged.AddListener((newVal) =>
                {
                    var x = newVal;
                    var y = cam.PointThreshold.Max.y;
                    var z = cam.PointThreshold.Max.z;
                    cam.PointThreshold.Max = new Vector3(x, y, z);
                });
                var pointThresholdSliderYMin = GameObject.Find($"PointThresholdYMin{i}")
                    .GetComponent<Slider>();
                var pointThresholdSliderYMax = GameObject.Find($"PointThresholdYMax{i}")
                    .GetComponent<Slider>();
                pointThresholdSliderYMin.onValueChanged.AddListener((newVal) =>
                {
                    var x = cam.PointThreshold.Min.x;
                    var y = newVal;
                    var z = cam.PointThreshold.Min.z;
                    cam.PointThreshold.Min = new Vector3(x, y, z);
                });
                pointThresholdSliderYMax.onValueChanged.AddListener((newVal) =>
                {
                    var x = cam.PointThreshold.Max.x;
                    var y = newVal;
                    var z = cam.PointThreshold.Max.z;
                    cam.PointThreshold.Max = new Vector3(x, y, z);
                });
                var pointThresholdSliderZMin = GameObject.Find($"PointThresholdZMin{i}")
                    .GetComponent<Slider>();
                var pointThresholdSliderZMax = GameObject.Find($"PointThresholdZMax{i}")
                    .GetComponent<Slider>();
                pointThresholdSliderZMin.onValueChanged.AddListener((newVal) =>
                {
                    var x = cam.PointThreshold.Min.x;
                    var y = cam.PointThreshold.Min.y;
                    var z = newVal;
                    cam.PointThreshold.Min = new Vector3(x, y, z);
                });
                pointThresholdSliderZMax.onValueChanged.AddListener((newVal) =>
                {
                    var x = cam.PointThreshold.Max.x;
                    var y = cam.PointThreshold.Max.y;
                    var z = newVal;
                    cam.PointThreshold.Max = new Vector3(x, y, z);
                });
                // var preTranslateSliderXCoarse = GameObject.Find($"PreTranslateXCoarse{i}")
                //     .GetComponent<Slider>();
                // var preTranslateSliderXFine = GameObject.Find($"PreTranslateXFine{i}")
                //     .GetComponent<Slider>();
                // preTranslateSliderXCoarse.onValueChanged.AddListener((coarseVal) =>
                // {
                //     var fineVal = preTranslateSliderXFine.value;
                //     var x = coarseVal + fineVal;
                //     var y = cam.PreTranslation.y;
                //     var z = cam.PreTranslation.z;
                //     cam.PreTranslation = new Vector3(x, y, z);
                // });
                // preTranslateSliderXFine.onValueChanged.AddListener((fineVal) =>
                // {
                //     var coarseVal = preTranslateSliderXCoarse.value;
                //     var x = coarseVal + fineVal;
                //     var y = cam.PreTranslation.y;
                //     var z = cam.PreTranslation.z;
                //     cam.PreTranslation = new Vector3(x, y, z);
                // });
                // var preTranslateSliderYCoarse = GameObject.Find($"PreTranslateYCoarse{i}")
                //     .GetComponent<Slider>();
                // var preTranslateSliderYFine = GameObject.Find($"PreTranslateYFine{i}")
                //     .GetComponent<Slider>();
                // preTranslateSliderYCoarse.onValueChanged.AddListener((coarseVal) =>
                // {
                //     var fineVal = preTranslateSliderYFine.value;
                //     var x = cam.PreTranslation.x;
                //     var y = coarseVal + fineVal;
                //     var z = cam.PreTranslation.z;
                //     cam.PreTranslation = new Vector3(x, y, z);
                // });
                // preTranslateSliderYFine.onValueChanged.AddListener((fineVal) =>
                // {
                //     var coarseVal = preTranslateSliderYCoarse.value;
                //     var x = cam.PreTranslation.x;
                //     var y = coarseVal + fineVal;
                //     var z = cam.PreTranslation.z;
                //     cam.PreTranslation = new Vector3(x, y, z);
                // });
                // var preTranslateSliderZCoarse = GameObject.Find($"PreTranslateZCoarse{i}")
                //     .GetComponent<Slider>();
                // var preTranslateSliderZFine = GameObject.Find($"PreTranslateZFine{i}")
                //     .GetComponent<Slider>();
                // preTranslateSliderZCoarse.onValueChanged.AddListener((coarseVal) =>
                // {
                //     var fineVal = preTranslateSliderZFine.value;
                //     var x = cam.PreTranslation.x;
                //     var y = cam.PreTranslation.y;
                //     var z = coarseVal + fineVal;
                //     cam.PreTranslation = new Vector3(x, y, z);
                // });
                // preTranslateSliderZFine.onValueChanged.AddListener((fineVal) =>
                // {
                //     var coarseVal = preTranslateSliderZCoarse.value;
                //     var x = cam.PreTranslation.x;
                //     var y = cam.PreTranslation.y;
                //     var z = coarseVal + fineVal;
                //     cam.PreTranslation = new Vector3(x, y, z);
                // });

                var postTranslateSliderXCoarse = GameObject.Find($"PostTranslateXCoarse{i}")
                    .GetComponent<Slider>();
                var postTranslateSliderXFine = GameObject.Find($"PostTranslateXFine{i}")
                    .GetComponent<Slider>();
                postTranslateSliderXCoarse.onValueChanged.AddListener((coarseVal) =>
                {
                    var fineVal = postTranslateSliderXFine.value;
                    var x = coarseVal + fineVal;
                    var y = cam.PostTranslation.y;
                    var z = cam.PostTranslation.z;
                    cam.PostTranslation = new Vector3(x, y, z);
                });
                postTranslateSliderXFine.onValueChanged.AddListener((fineVal) =>
                {
                    var coarseVal = postTranslateSliderXCoarse.value;
                    var x = coarseVal + fineVal;
                    var y = cam.PostTranslation.y;
                    var z = cam.PostTranslation.z;
                    cam.PostTranslation = new Vector3(x, y, z);
                });
                var postTranslateSliderYCoarse = GameObject.Find($"PostTranslateYCoarse{i}")
                    .GetComponent<Slider>();
                var postTranslateSliderYFine = GameObject.Find($"PostTranslateYFine{i}")
                    .GetComponent<Slider>();
                postTranslateSliderYCoarse.onValueChanged.AddListener((coarseVal) =>
                {
                    var fineVal = postTranslateSliderYFine.value;
                    var x = cam.PostTranslation.x;
                    var y = coarseVal + fineVal;
                    var z = cam.PostTranslation.z;
                    cam.PostTranslation = new Vector3(x, y, z);
                });
                postTranslateSliderYFine.onValueChanged.AddListener((fineVal) =>
                {
                    var coarseVal = postTranslateSliderYCoarse.value;
                    var x = cam.PostTranslation.x;
                    var y = coarseVal + fineVal;
                    var z = cam.PostTranslation.z;
                    cam.PostTranslation = new Vector3(x, y, z);
                });
                var postTranslateSliderZCoarse = GameObject.Find($"PostTranslateZCoarse{i}")
                    .GetComponent<Slider>();
                var postTranslateSliderZFine = GameObject.Find($"PostTranslateZFine{i}")
                    .GetComponent<Slider>();
                postTranslateSliderZCoarse.onValueChanged.AddListener((coarseVal) =>
                {
                    var fineVal = postTranslateSliderZFine.value;
                    var x = cam.PostTranslation.x;
                    var y = cam.PostTranslation.y;
                    var z = coarseVal + fineVal;
                    cam.PostTranslation = new Vector3(x, y, z);
                });
                postTranslateSliderZFine.onValueChanged.AddListener((fineVal) =>
                {
                    var coarseVal = postTranslateSliderZCoarse.value;
                    var x = cam.PostTranslation.x;
                    var y = cam.PostTranslation.y;
                    var z = coarseVal + fineVal;
                    cam.PostTranslation = new Vector3(x, y, z);
                });

                var rotateSliderXCoarse = GameObject.Find($"RotateXCoarse{i}")
                    .GetComponent<Slider>();
                var rotateSliderXFine = GameObject.Find($"RotateXFine{i}")
                    .GetComponent<Slider>();
                rotateSliderXCoarse.onValueChanged.AddListener((coarseVal) =>
                {
                    var fineVal = rotateSliderXFine.value;
                    var x = (coarseVal * 36) + (fineVal * 1);
                    var y = cam.Rotation.y;
                    var z = cam.Rotation.z;
                    cam.Rotation = new Vector3(x, y, z);
                });
                rotateSliderXFine.onValueChanged.AddListener((fineVal) =>
                {
                    var coarseVal = rotateSliderXCoarse.value;
                    var x = (coarseVal * 36) + (fineVal * 1);
                    var y = cam.Rotation.y;
                    var z = cam.Rotation.z;
                    cam.Rotation = new Vector3(x, y, z);
                });
                var rotateSliderYCoarse = GameObject.Find($"RotateYCoarse{i}")
                    .GetComponent<Slider>();
                var rotateSliderYFine = GameObject.Find($"RotateYFine{i}")
                    .GetComponent<Slider>();
                rotateSliderYCoarse.onValueChanged.AddListener((coarseVal) =>
                {
                    var fineVal = rotateSliderYFine.value;
                    var x = cam.Rotation.x;
                    var y = (coarseVal * 36) + (fineVal * 1);
                    var z = cam.Rotation.z;
                    cam.Rotation = new Vector3(x, y, z);
                });
                rotateSliderYFine.onValueChanged.AddListener((fineVal) =>
                {
                    var coarseVal = rotateSliderYCoarse.value;
                    var x = cam.Rotation.x;
                    var y = (coarseVal * 36) + (fineVal * 1);
                    var z = cam.Rotation.z;
                    cam.Rotation = new Vector3(x, y, z);
                });
                var rotateSliderZCoarse = GameObject.Find($"RotateZCoarse{i}")
                    .GetComponent<Slider>();
                var rotateSliderZFine = GameObject.Find($"RotateZFine{i}")
                    .GetComponent<Slider>();
                rotateSliderZCoarse.onValueChanged.AddListener((coarseVal) =>
                {
                    var fineVal = rotateSliderZFine.value;
                    var x = cam.Rotation.x;
                    var y = cam.Rotation.y;
                    var z = (coarseVal * 36) + (fineVal * 1);
                    cam.Rotation = new Vector3(x, y, z);
                });
                rotateSliderZFine.onValueChanged.AddListener((fineVal) =>
                {
                    var coarseVal = rotateSliderZCoarse.value;
                    var x = cam.Rotation.x;
                    var y = cam.Rotation.y;
                    var z = (coarseVal * 36) + (fineVal * 1);
                    cam.Rotation = new Vector3(x, y, z);
                });
            }

            var calibrationButton = GameObject.Find("CalibrateButton")
                .GetComponent<Button>();
            calibrationButton.onClick.AddListener(() => RunCalibration());

            var pauseAllButton = GameObject.Find("PauseAllButton")
                .GetComponent<Button>();
            pauseAllButton.onClick.AddListener(() => {
                for (int i = 0; i < CameraCount; i++)
                {
                    var pauseButton = GameObject.Find($"PauseButton{i}")
                        .GetComponent<Button>();
                    pauseButton.onClick.Invoke();
                }
                var pauseAllLabel = pauseAllButton.GetComponentsInChildren<Text>().First();
                pauseAllLabel.text = Cameras.First().Paused ? "Resume All Cameras" : "Pause All Cameras";
            });

        }
    }
}
