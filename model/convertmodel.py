from onnxmltools import convert_coreml
from coremltools.models.utils import load_spec

model_coreml = load_spec('cvmodel.mlmodel')  
model_onnx = convert_coreml(model_coreml, name='cvmodel') 