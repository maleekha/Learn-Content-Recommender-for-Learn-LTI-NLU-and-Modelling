from sentence_transformers import SentenceTransformer
import torch
from azureml.core.model import Model
import argparse
from azureml.core.authentication import InteractiveLoginAuthentication


parser = argparse.ArgumentParser()
parser.add_argument("--resourceGroupName", help="Enter the resource group name")
parser.add_argument("--subscriptionId", help="Enter the subscription id")
parser.add_argument("--workspaceName", help="Enter the azure ML workspace name")
parser.add_argument("--tenantId", help="Tenant ID")
args = parser.parse_args()
print(args)

model = SentenceTransformer('distilbert-base-nli-mean-tokens')
torch.save(model, 'model.pt')
interactive_auth = InteractiveLoginAuthentication(tenant_id=args.tenantId)

# register the model to the workspce
from azureml.core import Workspace
ws = Workspace(args.subscriptionId, args.resourceGroupName,  args.workspaceName, auth=interactive_auth)
model = Model.register(workspace=ws, model_path="model.pt", model_name="learn_recommender_model")

# create a service endpoint for the registered model





