from winmltools import convert_coreml
from coremltools.models.utils import load_spec
from winmltools.utils import save_model
from winmltools.utils import save_text

model_coreml = load_spec('inkshapes.mlmodel')  
model_onnx = convert_coreml(model_coreml, name='inkshapes') 
save_model(model_onnx, 'inkshapes.onnx')
save_text(model_onnx, 'inkshapes.txt')