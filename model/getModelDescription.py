from coremltools.models.utils import load_spec
model_coreml = load_spec('checkout.mlmodel')
print(model_coreml.description) # Print the content of Core ML model description