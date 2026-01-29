# Practice Logger Infrastructure

This repo contains a minimal CloudFormation template that provisions:
- A new VPC with public and private subnets
- An ECS Fargate service for the frontend (public)
- A private API Gateway + Lambda for the API
- RDS PostgreSQL
- An ECR repository for the frontend container image

## Prerequisites
- AWS CLI configured with credentials
- Docker
- An AWS account with permissions to create VPC/ECS/ALB/RDS/ECR/IAM resources

## Build and push frontend image (ECR)
Set your region and stack name once:

```bash
export AWS_REGION=us-east-1
export STACK_NAME=practice-logger
```

Deploy the stack first to create the ECR repos:

```bash
aws cloudformation deploy \
  --stack-name "$STACK_NAME" \
  --template-file cloudformation.yaml \
  --capabilities CAPABILITY_NAMED_IAM \
  --region "$AWS_REGION"
```

Fetch the repository URIs:

```bash
FRONTEND_REPO=$(aws cloudformation describe-stacks \
  --stack-name "$STACK_NAME" \
  --query "Stacks[0].Outputs[?OutputKey=='FrontendRepositoryUri'].OutputValue" \
  --output text \
  --region "$AWS_REGION")
```

Login to ECR:

```bash
aws ecr get-login-password --region "$AWS_REGION" \
  | docker login --username AWS --password-stdin "$(echo "$FRONTEND_REPO" | cut -d/ -f1)"
```

Build, tag, and push images:

```bash
docker build -t practice-logger-frontend ../practice-logger-ui

docker tag practice-logger-frontend:latest "$FRONTEND_REPO":latest

docker push "$FRONTEND_REPO":latest
```

## Deploy API Lambda
The API Lambda is deployed by GitHub Actions. The workflow should upload the Lambda package to S3 and then deploy/update the stack with the S3 bucket/key plus runtime/handler parameters.

## Deploy the stack
Deploy again, passing image tags if needed:

```bash
aws cloudformation deploy \
  --stack-name "$STACK_NAME" \
  --template-file cloudformation.yaml \
  --capabilities CAPABILITY_NAMED_IAM \
  --parameter-overrides \
    FrontendImageTag=latest \
    ApiLambdaS3Bucket=your-artifact-bucket \
    ApiLambdaS3Key=path/to/api-lambda.zip \
    ApiLambdaHandler=Your.Namespace::Your.LambdaEntryPoint::FunctionHandlerAsync \
    ApiLambdaRuntime=dotnet8 \
  --region "$AWS_REGION"
```

## Outputs
After deploy, fetch the public frontend URL:

```bash
aws cloudformation describe-stacks \
  --stack-name "$STACK_NAME" \
  --query "Stacks[0].Outputs[?OutputKey=='FrontendAlbDnsName'].OutputValue" \
  --output text \
  --region "$AWS_REGION"
```

Note: The API is private. The frontend is expected to call the API from within the VPC.

Private API Gateway endpoint (reachable only from within the VPC):

```bash
aws cloudformation describe-stacks \
  --stack-name "$STACK_NAME" \
  --query "Stacks[0].Outputs[?OutputKey=='ApiRestApiEndpoint'].OutputValue" \
  --output text \
  --region "$AWS_REGION"
```
