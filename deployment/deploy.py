from sentence_transformers import SentenceTransformer
import torch
import argparse
from azureml.core.authentication import InteractiveLoginAuthentication
from transformers import BertModel
from azureml.core import Workspace
from azureml.core.model import InferenceConfig
from azureml.core.environment import Environment
from azureml.core.conda_dependencies import CondaDependencies
from azureml.core.webservice import AciWebservice
from azureml.core.model import Model

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--resourceGroupName", help="Enter the resource group name")
    parser.add_argument("--subscriptionId", help="Enter the subscription id")
    parser.add_argument("--workspaceName", help="Enter the azure ML workspace name")
    parser.add_argument("--tenantId", help="Tenant ID")
    args = parser.parse_args()

    #save the model
    bert_model = BertModel.from_pretrained('bert-base-uncased') 
    torch.save(bert_model, 'model.pt')

    # register the model to the workspce
    interactive_auth = InteractiveLoginAuthentication(tenant_id=args.tenantId)
    ws = Workspace(args.subscriptionId, args.resourceGroupName,  args.workspaceName, auth=interactive_auth)
    model = Model.register(workspace=ws, model_path="model.pt", model_name="learn_recommender_modeltest")

    # create a service endpoint for the registered model

    myenv = Environment.from_conda_specification(name = "myenv", file_path = "environment.yml")
    myenv.docker.enabled = True

    ws = Workspace(args.subscriptionId, args.resourceGroupName, args.workspaceName)
    inference_config = InferenceConfig(entry_script="score.py", environment=myenv)

    aci_config = AciWebservice.deploy_configuration(cpu_cores=1, memory_gb=1.5)
    aci_config.scoring_timeout_ms=1200000

    service = Model.deploy(ws, "bert2", [Model.list(ws)[0]], inference_config, deployment_config=aci_config)
    service.wait_for_deployment(show_output=True)
    print(service.scoring_uri)

if __name__ == "__main__":
    main()



