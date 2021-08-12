@Library("JenkinsPipelineLibrary") _

pipeline {
  agent any

  options{
    buildDiscarder(logRotator(numToKeepStr: '5'))    
    disableConcurrentBuilds()
  }

  triggers{
    pollSCM("H/15 * * * *")
  } 

  stages {
    stage('Build') {
      steps {        
        script{
          pipelineLib.beginSonarQubeForMsBuild("LAP_IdentityServer", "Legion / LAP.IdentityServer", "/d:sonar.cs.opencover.reportsPaths=%WORKSPACE%\\CoverageReports\\*.xml /d:sonar.cs.vstest.reportsPaths=%WORKSPACE%\\TestResults\\*.trx")

          pipelineLib.msBuild16("Fanex.Athena.IdentityServer.sln")
        }
      }
    }
   
     stage("Test"){
      parallel{
        stage("C# Unit Test"){
          steps{
            script{
              pipelineLib.xUnitForNet5()
            }
          }
        }
		
      }      
    }    

    stage("SonarQube Analysis"){
      steps{       
        script{
          pipelineLib.endSonarQubeForMsBuild()
        }
      }
      
      post{
        always{
          script{
            if(manager.logContains(".*Quality gate status.*")){              
              pipelineLib.generateSonarQubeReport("LAP_IdentityServer")
            }
          }
        }
      }
    }
  }

  post{
    always{
      archiveArtifacts "*.xml,*.email"
      step([$class: 'ACIPluginPublisher', name: '*.xml', shownOnProjectPage: false])      
      mstest failOnError: false
    }
    unsuccessful{
      emailext body: '$DEFAULT_CONTENT', subject: '$DEFAULT_SUBJECT', to: 'kos.nguyen@starixvn.com,howard.nguyen@starixvn.com,hogan.pham@starixvn.com'
    }
  }
}
