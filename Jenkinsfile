pipeline {
    agent any

    environment {
        // SONARQUBE_HOME = tool 'sonarqube-scanner'
        NAME='techno-frontend-hotel-service'
        IMAGE_TAG='latest'
        DOCKERFILE='Dockerfile'
        REGISTRY = "localhost:5100"
        DOCKER_REPO='repository/docker-hosted-private'
        IMAGE_NAME = "${REGISTRY}/${DOCKER_REPO}/${NAME}"
        CREDENTIALS_ID = "nexus-docker-credentials"
    }

    stages {
        stage('Checkout') {
            steps {
                git branch: 'main', credentialsId: 'github-credential', url: 'https://github.com/technoheavenbilimora/techno-frontend-hotel-service.git'
            }
        }

        stage('Restore') {
            steps {
                sh 'dotnet restore'
            }
        }

         stage('GitLeak Security ') {
            steps {
                sh 'gitleaks detect --source . --verbose --no-git --redact  -f json --exit-code 0'
                // sh 'gitleaks detect --source . --verbose --no-git --redact  -f json -r gitleaks-report.json --exit-code 0'
            }
        }

        stage('Trivy scane'){
            steps{
                sh 'trivy fs . --format table'
                sh 'trivy fs . --format table --scanners config'
                sh 'trivy fs . --format table --scanners secret'
                // sh 'trivy fs . --format table -o trivy-reports.html'
                // sh 'trivy fs . --format table -o trivy-reports-config.html --scanners config'
                // sh 'trivy fs . --format table -o trivy-reports-config.html --scanners secret'
            }
        }
        
        // stage('SonarQube Analysis') {
        //         steps {
        //             withSonarQubeEnv('sonarqube-server') { 
        //             sh '''
        //                 $SONARQUBE_HOME/bin/sonar-scanner \
        //                 -Dsonar.projectKey=${NAME} \
        //                 -Dsonar.projectName=${NAME} \
        //                 -Dsonar.sources=. \
        //                 -Dsonar.projectVersion=${BUILD_NUMBER} \
        //                 -Dsonar.branch.name=main \
        //             '''
        //             } 
        //       }
        // }

        stage('Build') {
            steps {
                sh 'dotnet build'
            }
        }

        stage('Test') {
            steps {
                sh 'dotnet test'
            }
        }

        stage('Build docker Images') {
            steps {
                sh 'docker build -t ${IMAGE_NAME}:${IMAGE_TAG} -f ${DOCKERFILE} . '
            }
        }

        stage('Docker Images'){
            steps {
                    // sh 'docker images'
                    sh 'docker images | grep ${IMAGE_NAME}'
                }
        }

        stage('Trivy Image scane'){
            steps{
                    sh 'trivy image ${IMAGE_NAME}:${IMAGE_TAG} --format table'
                    sh 'trivy image ${IMAGE_NAME}:${IMAGE_TAG} --format table --scanners config'
                    sh 'trivy image ${IMAGE_NAME}:${IMAGE_TAG} --format table --scanners secret'
                    // sh 'trivy image ${IMAGE_NAME}:${IMAGE_TAG} --format table -o trivy-image-reports.html'
                    // sh 'trivy image ${IMAGE_NAME}:${IMAGE_TAG} --format table -o trivy-image-reports-config.html --scanners config'
                    // sh 'trivy image ${IMAGE_NAME}:${IMAGE_TAG} --format table -o trivy-image-reports-secret.html --scanners secret'
                }
        }

        stage('Login to Nexus Docker Registry') {
            steps {
                script {
                    docker.withRegistry("http://${REGISTRY}", "${CREDENTIALS_ID}") {
                        docker.image("${IMAGE_NAME}:${IMAGE_TAG}").push()
                    }
                }
            }
        }

        stage('Logout from Nexus Docker Registry') {
            steps {
                sh 'docker logout ${REGISTRY}'
            }
        }

        stage('Rollout Restart Deployment in Kubernetes' ) {
            steps {
                script {
                    sh '''
                        kubectl rollout restart deployment hotelservice-deploy
                        sleep 10
                        kubectl get pods --selector=app=hotelservice-app
                    '''
                }
            }
        }

        stage('Email Notification') {
            steps {
                echo 'Email Notification will be sent after the completion of the pipeline'
            }
        }
        
        stage('Cleanup Image') {
            steps {
                sh 'docker rmi ${IMAGE_NAME}:${IMAGE_TAG} || true'
            }
        }
        
    }

    post {
        success {
            echo "Pipeline execute successfully."
            // emailext(
            //     subject: "${NAME} Jenkins pipline successfully executed: ${BUILD_NUMBER}",
            //     body: '''
            //         <html>
            //         <body>
            //             <h3>Congratulations!</h3>
            //             <p>The build <strong>${JOB_NAME} ${BUILD_NUMBER}</strong> has completed successfully.</p>
            //             <p>Click below to view the Sonarqube results:</p>
            //             <p><a href="http://172.10.1.154:9000/dashboard?id=autocomplete&branch=main">Sonarqube Results</a></p>
            //             <p>Thank you!</p>
            //         </body>
            //         </html>
            //     ''',
            //     to: 'Vibhuti.d@technoheaven.net,abhishek@technoheaven.net',
            //     mimeType: 'text/html',
            //     attachmentsPattern: 'gitleaks-report.txt,trivy-report.html'
            // )
        }
        failure {
            echo "Pipeline failed."
            // emailext(
            //     subject: "${NAME} Jenkins pipeline failed: ${BUILD_NUMBER}",
            //     body: '''
            //         <html>
            //         <body>
            //             <h3>Pipeline failed!</h3>
            //             <p>The build <strong>${JOB_NAME} ${BUILD_NUMBER}</strong> has failed.</p>
            //             <p>Click below to view the Sonarqube results:</p>
            //             <p><a href="http://172.10.1.154:9000/dashboard?id=autocomplete">Sonarqube Results</a></p>
            //             <p>Thank you!</p>
            //         </body>
            //         </html>
            //     ''',
            //     to : 'Vibhuti.d@technoheaven.net,abhishek@technoheaven.net',
            //     mimeType: 'text/html',
            //     attachmentsPattern: 'gitleaks-report.txt,trivy-report.html'
            // )
        }
    }
}