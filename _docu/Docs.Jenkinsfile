pipeline {
    agent any
    environment {      
        GITEA_URL = "gitea.ekkodale.biz"
        GITEA_OWNER = "ekkodale-GmbH"
    }
    stages {

        
        /**
         * This stage cleans the workspace by deleting all files and folders except the '_docu' folder.
         */
        stage('Clean workspace') {
            steps {
                // Delete all other files and folders except _docu folder
                bat 'powershell.exe -Command "Get-ChildItem -Exclude \'_docu\' | Remove-Item -Recurse -Force"'
            }
        }

        /**
         * This stage pushes files to another repository. It clones the target repository, 
         * copies files from specific folders in the source repository to the target repository.
         * 
         * Credentials are required to access the target repository.
         */
        stage('Push files to another repository') {
            steps {
                script {
                    withCredentials([usernamePassword(credentialsId: 'a4ce2ea9-ba58-44cc-a832-2a5796b6e277', passwordVariable: 'EKKODALE_GITEA_PASSWORD', usernameVariable: 'EKKODALE_GITEA_USERNAME')]) 
                    {
                        // Clone the target repository
                        checkout([
                            $class: 'GitSCM',
                            branches: [[name: '*/main']],
                            userRemoteConfigs: [[
                                url: 'https://gitea.ekkodale.biz/ekkodale-GmbH/Public-Documentation.git',
                                credentialsId: 'a4ce2ea9-ba58-44cc-a832-2a5796b6e277'
                            ]],
                            extensions: [[$class: 'RelativeTargetDirectory', relativeTargetDir: 'git']]
                        ])

                        // Copy files from specific folders in the source repository to the target repository
                        bat 'xcopy _docu\\user_docs\\* git /s /e /y'
                    }
                }
            }
        }

        // This stage pushes the changes to the target repository using Git.
        stage("Pushing with Git"){
            steps{
                dir("git") {
                    // Commit and push the changes to the target repository
                    bat """
                        git checkout main
                        git add .
                        git -c user.email="jenkins@ekkodale.com" -c user.name="Jenkins" commit -m "Updated guideline documentation from ${BUILD_URL}"
                        git push
                    """
                }
            }
        }
    }
    post{
       always { 
           cleanWs(cleanWhenNotBuilt: false,
            cleanWhenAborted: false,
            cleanWhenFailure: false,
            cleanWhenUnstable: false,
            deleteDirs: true,
            notFailBuild: true)
       }
    }
}